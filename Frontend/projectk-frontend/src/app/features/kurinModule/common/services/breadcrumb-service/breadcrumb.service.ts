import { inject, Injectable } from '@angular/core';
import { ActivatedRoute, NavigationEnd, Route, Router } from '@angular/router';
import { MenuItem } from '@openng/optimus-ui/api';
import { BehaviorSubject, Observable, catchError, filter, of } from 'rxjs';
import { authenticatedHomeRoute } from '../../../../authModule/functions/authenticated-home-route';
import { AuthState } from '../../../../authModule/models/auth-state.model';
import { AuthService } from '../../../../authModule/services/authService/auth.service';
import { PermissionService } from '../../../../authModule/services/permission.service';
import { isUsableKey } from '../../../../../shared/functions/isUsableKey.function';
import {
  TitleContextType,
  formatGroupTitle,
  formatKurinTitle,
  formatMemberTitle
} from '../../../../systemModule/services/page-title.format';
import { GroupService } from '../group-service/group.service';
import { KurinService } from '../kurin-service/kurin.service';
import { MemberService } from '../member-service/member.service';

interface EntityTarget {
  type: TitleContextType;
  key: string;
}

@Injectable({
  providedIn: 'root'
})
export class BreadcrumbService {
  private breadcrumbsSubject = new BehaviorSubject<MenuItem[]>([]);
  public breadcrumbs$: Observable<MenuItem[]> = this.breadcrumbsSubject.asObservable();
  private homeSubject = new BehaviorSubject<MenuItem>({ icon: 'pi pi-home' });
  public home$: Observable<MenuItem> = this.homeSubject.asObservable();
  private paramCache: Record<string, string> = {};
  private entityTargets = new Map<string, EntityTarget>();
  private entityLabels = new Map<string, string>();
  private requestedEntities = new Set<string>();
  private navigationToken = 0;
  private readonly router = inject(Router);
  private readonly activatedRoute = inject(ActivatedRoute);
  private readonly permissionService = inject(PermissionService);
  private readonly authService = inject(AuthService);
  private readonly kurinService = inject(KurinService);
  private readonly groupService = inject(GroupService);
  private readonly memberService = inject(MemberService);

  constructor() {
    if (this.router && this.router.events) {
      this.router.events.pipe(
        filter(event => event instanceof NavigationEnd)
      ).subscribe(() => this.refresh());
    }

    // The service is built on first injection, which is whenever the component holding
    // the breadcrumb first renders. If that is after the initial navigation — the
    // toolbar only appears once auth resolves — the NavigationEnd above never fires for
    // the page the user actually landed on, and the trail would stay empty until they
    // navigated somewhere else.
    this.refresh();
  }

  private refresh(): void {
    this.updateParamCache();
    // Names are dropped on every navigation so a rename never survives as a stale crumb;
    // ClientCacheService keeps the refetch off the network for the next minute anyway.
    this.entityLabels.clear();
    this.requestedEntities.clear();
    this.navigationToken++;
    this.homeSubject.next(this.createHome());
    this.breadcrumbsSubject.next(this.createBreadcrumbs());
    this.requestEntityLabels();
  }

  private createHome(): MenuItem {
    const state = this.authService.getAuthStateValue?.() ?? null;
    const label = this.homeLabel(state);

    return {
      icon: 'pi pi-home',
      routerLink: authenticatedHomeRoute(state),
      title: label,
      tooltipOptions: { tooltipLabel: label, tooltipPosition: 'bottom' }
    };
  }

  // Mirrors authenticatedHomeRoute: the icon has to name the place it actually leads to.
  private homeLabel(state: AuthState | null): string {
    if (isUsableKey(state?.kurinKey)) {
      return 'Курінь';
    }

    if (this.permissionService.isAdmin()) {
      return 'Адміністрація';
    }

    return isUsableKey(state?.memberKey) ? 'Моя картка' : 'На початок';
  }

  public setParam(key: string, value: string): void {
    if (!isUsableKey(value) || this.paramCache[key] === value) {
      return;
    }

    this.paramCache[key] = value;
    const breadcrumbs = this.createBreadcrumbs();
    this.breadcrumbsSubject.next(breadcrumbs);
    this.requestEntityLabels();
  }

  // A mentor with no group carries Guid.Empty as their groupKey. Caching it would build a
  // crumb linking to /group/00000000-…, which every access check rejects — so placeholder
  // keys never enter the cache and the crumb that needs them is simply not built.
  private cacheParams(params: Record<string, string>): void {
    for (const [key, value] of Object.entries(params)) {
      if (isUsableKey(value)) {
        this.paramCache[key] = value;
      }
    }
  }

  private updateParamCache(): void {
    // Route parameters are navigation-scoped. Dynamic values such as a member's
    // groupKey may be injected again after the destination entity is loaded.
    this.paramCache = {};

    // Extract parameters from URL segments
    const urlSegments = (this.router.url ?? '').split('/').filter(s => s);
    const routes = this.router.config ?? [];
    
    // Try to match URL segments to route patterns to extract parameters
    for (const route of routes) {
      if (!route.path) continue;
      
      const routeSegments = route.path.split('/').filter(s => s);
      if (routeSegments.length <= urlSegments.length) {
        let match = true;
        const extractedParams: Record<string, string> = {};
        
        for (let i = 0; i < routeSegments.length; i++) {
          if (routeSegments[i].startsWith(':')) {
            // This is a parameter - extract it
            const paramName = routeSegments[i].substring(1);
            extractedParams[paramName] = urlSegments[i];
          } else if (routeSegments[i] !== urlSegments[i]) {
            // Static segment doesn't match
            match = false;
            break;
          }
        }
        
        if (match) {
          // Add extracted parameters to cache
          this.cacheParams(extractedParams);
        }
      }
    }
    
    // Also add parameters from the activated route
    this.addParamsFromRoute(this.activatedRoute);
  }
  
  private addParamsFromRoute(route: ActivatedRoute): void {
    if (!route?.snapshot) {
      return;
    }

    // Add params from current route
    this.cacheParams(route.snapshot.params);

    // Process children
    if (route.firstChild) {
      this.addParamsFromRoute(route.firstChild);
    }

    // Process siblings
    (route.children ?? []).forEach(child => {
      this.addParamsFromRoute(child);
    });
  }

  private createBreadcrumbs(): MenuItem[] {
    const breadcrumbs: MenuItem[] = [];
    this.entityTargets.clear();

    // Get the current activated route
    let currentRoute: ActivatedRoute = this.activatedRoute;
    if (!currentRoute) {
      return breadcrumbs;
    }
    while (currentRoute.firstChild) {
      currentRoute = currentRoute.firstChild;
    }

    // Process the current route
    this.processRoute(currentRoute, breadcrumbs);

    return this.applyEntityLabels(this.dropCrumbsUpToHome(breadcrumbs));
  }

  // A crumb that stands for an entity carries the entity's own name — the same one the
  // page title shows — and falls back to the static route label until it is loaded.
  private applyEntityLabels(breadcrumbs: MenuItem[]): MenuItem[] {
    return breadcrumbs.map(item => {
      const target = this.entityTargets.get(this.normalizePath(item.routerLink));
      const label = target ? this.entityLabels.get(this.entityCacheKey(target)) : undefined;
      return label ? { ...item, label } : item;
    });
  }

  private trackEntity(path: string, data: Record<string, unknown> | undefined): void {
    const type = data?.['breadcrumbEntity'];
    if (type !== 'kurin' && type !== 'group' && type !== 'member') {
      return;
    }

    const key = this.entityKey(type);
    if (key) {
      this.entityTargets.set(this.normalizePath(path), { type, key });
    }
  }

  private entityKey(type: TitleContextType): string | null {
    switch (type) {
      case 'group':
        return this.paramCache['groupKey'] ?? null;
      case 'member':
        return this.paramCache['memberKey'] ?? null;
      case 'kurin': {
        const scopedKurinKey = this.authService.getAuthStateValue?.()?.kurinKey;
        return this.paramCache['kurinKey'] ?? (isUsableKey(scopedKurinKey) ? scopedKurinKey : null);
      }
    }
  }

  private entityCacheKey(target: EntityTarget): string {
    return `${target.type}:${target.key}`;
  }

  private requestEntityLabels(): void {
    const token = this.navigationToken;

    for (const target of this.entityTargets.values()) {
      const cacheKey = this.entityCacheKey(target);
      if (this.requestedEntities.has(cacheKey)) {
        continue;
      }
      this.requestedEntities.add(cacheKey);

      switch (target.type) {
        case 'kurin':
          this.publishLabel(token, cacheKey, this.kurinService.getByKey(target.key), kurin => formatKurinTitle(kurin.number));
          break;
        case 'group':
          this.publishLabel(token, cacheKey, this.groupService.getByKey(target.key), group => formatGroupTitle(group.name));
          break;
        case 'member':
          this.publishLabel(token, cacheKey, this.memberService.getByKey(target.key), member =>
            formatMemberTitle(member.lastName, member.firstName)
          );
          break;
      }
    }
  }

  private publishLabel<T>(
    token: number,
    cacheKey: string,
    source: Observable<T>,
    format: (entity: T) => string | null
  ): void {
    source
      .pipe(catchError(() => of(null)))
      .subscribe(entity => {
        // A response from the page the user already left must not relabel the new trail.
        if (entity === null || token !== this.navigationToken) {
          return;
        }

        const label = format(entity);
        if (!label || this.entityLabels.get(cacheKey) === label) {
          return;
        }

        this.entityLabels.set(cacheKey, label);
        this.breadcrumbsSubject.next(this.createBreadcrumbs());
      });
  }

  // The house icon already leads to the user's root, so anything at or above it is dead
  // weight: a second link to the same page, and — for a kurin-scoped user — an
  // "Адміністрація" crumb that kurinAccessGuard bounces straight back to the kurin.
  private dropCrumbsUpToHome(breadcrumbs: MenuItem[]): MenuItem[] {
    const state = this.authService.getAuthStateValue?.() ?? null;
    const homePath = this.normalizePath(authenticatedHomeRoute(state));

    for (let i = breadcrumbs.length - 1; i >= 0; i--) {
      if (this.normalizePath(breadcrumbs[i].routerLink) === homePath) {
        return breadcrumbs.slice(i + 1);
      }
    }

    return breadcrumbs;
  }

  private toPath(segments: unknown[]): string {
    return segments.map(segment => String(segment)).join('/');
  }

  private normalizePath(link: unknown): string {
    const raw = Array.isArray(link) ? this.toPath(link) : String(link ?? '');
    return raw.split(/[?#]/)[0].split('/').filter(segment => segment).join('/');
  }

  private processRoute(route: ActivatedRoute, breadcrumbs: MenuItem[]): void {
    if (!route?.snapshot?.data) return;

    // If this route has breadcrumb data
    if (route.snapshot.data['breadcrumb']) {
      // Create breadcrumb item for current route
      const currentUrl = this.router.url ?? '';
      const currentItem: MenuItem = {
        label: route.snapshot.data['breadcrumb'],
        routerLink: currentUrl
      };
      
      // Add to the beginning of the array
      breadcrumbs.unshift(currentItem);
      this.trackEntity(currentUrl, route.snapshot.data);

      // Process parent if exists
      const parentPath = this.resolveParentPath(
        route.snapshot.data['parent'],
        route.snapshot.data['parentFallback']
      );
      if (parentPath && this.isParentAllowed(route.snapshot.data['parentRoles'])) {
        this.processParent(parentPath, breadcrumbs);
      }
    }
  }

  private resolveParentPath(parent: unknown, fallback: unknown): string | null {
    if (typeof parent !== 'string' || parent.length === 0) {
      return null;
    }

    const resolvedParent = this.resolveParameters(parent);
    if (!resolvedParent.includes(':')) {
      return parent;
    }

    return typeof fallback === 'string' && fallback.length > 0
      ? fallback
      : parent;
  }
  
  private processParent(parentPath: string, breadcrumbs: MenuItem[]): void {
    if (!parentPath) return;

    // Resolve any parameters in the parent path
    const resolvedPath = this.resolveParameters(parentPath);

    // Find the route configuration for this path
    const route = this.findRouteByPattern(parentPath);
    if (route && route.data?.['breadcrumb']) {
      // A path we could not fill in is a dead link — skip the crumb, but keep walking up
      // so the trail still reaches somewhere the user can actually go.
      if (!resolvedPath.includes(':')) {
        // Create breadcrumb item for the parent
        const parentItem: MenuItem = {
          label: route.data['breadcrumb'],
          routerLink: resolvedPath
        };

        // Add to the beginning of the array
        breadcrumbs.unshift(parentItem);
        this.trackEntity(resolvedPath, route.data);
      }

      // Process grandparent if exists
      if (route.data['parent'] && this.isParentAllowed(route.data['parentRoles'])) {
        this.processParent(route.data['parent'], breadcrumbs);
      }
    }
  }

  private isParentAllowed(parentRoles: unknown): boolean {
    if (!Array.isArray(parentRoles) || parentRoles.length === 0) {
      return true;
    }

    return parentRoles.some(role => {
      if (typeof role !== 'string') {
        return false;
      }
      switch (role.trim().toLowerCase()) {
        case 'admin': return this.permissionService.isAdmin();
        case 'manager': return this.permissionService.isManager();
        case 'mentor': return this.permissionService.isMentor();
        default: return false;
      }
    });
  }
  
  private resolveParameters(path: string): string {
    if (!path) return '';
    
    let result = path;
    const paramMatches = path.match(/:[a-zA-Z0-9]+/g) || [];
    
    for (const param of paramMatches) {
      const paramName = param.substring(1); // Remove the colon
      if (this.paramCache[paramName]) {
        result = result.replace(param, this.paramCache[paramName]);
      }
    }
    
    return result;
  }
  
  private findRouteByPattern(pathPattern: string): Route | null {
    if (!pathPattern) return null;

    // Remove leading slash if present
    const normalizedPattern = pathPattern.startsWith('/') ? pathPattern.substring(1) : pathPattern;
    
    // Convert parameters to regex pattern
    const regexPattern = normalizedPattern
      .replaceAll(/\//g, '\\/') // Escape slashes
      .replaceAll(/:[a-zA-Z0-9]+/g, '[^\\/]+'); // Replace params with wildcard pattern
    
    for (const route of this.router.config ?? []) {
      if (!route.path) continue;
      
      // Direct match
      if (route.path === normalizedPattern) {
        return route;
      }
      
      // Pattern match
      if (new RegExp(`^${regexPattern}$`).test(route.path) || 
          new RegExp(`^${route.path.replaceAll(/:[a-zA-Z0-9]+/g, '[^\\/]+')}$`).test(normalizedPattern)) {
        return route;
      }
    }
    
    return null;
  }
}
