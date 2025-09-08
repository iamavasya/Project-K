import { Injectable } from '@angular/core';
import { ActivatedRoute, NavigationEnd, Router } from '@angular/router';
import { MenuItem } from 'primeng/api';
import { BehaviorSubject, Observable, filter } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class BreadcrumbService {
  private breadcrumbsSubject = new BehaviorSubject<MenuItem[]>([]);
  public breadcrumbs$: Observable<MenuItem[]> = this.breadcrumbsSubject.asObservable();
  private paramCache: Record<string, string> = {};

  constructor(private router: Router, private activatedRoute: ActivatedRoute) {
    this.router.events.pipe(
      filter(event => event instanceof NavigationEnd)
    ).subscribe(() => {
      // Update param cache with parameters from the URL
      this.updateParamCache();
      
      // Create breadcrumbs
      const breadcrumbs = this.createBreadcrumbs();
      this.breadcrumbsSubject.next(breadcrumbs);
    });
  }

  private updateParamCache(): void {
    // Extract parameters from URL segments
    const urlSegments = this.router.url.split('/').filter(s => s);
    const routes = this.router.config;
    
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
          Object.assign(this.paramCache, extractedParams);
        }
      }
    }
    
    // Also add parameters from the activated route
    this.addParamsFromRoute(this.activatedRoute);
  }
  
  private addParamsFromRoute(route: ActivatedRoute): void {
    // Add params from current route
    Object.assign(this.paramCache, route.snapshot.params);
    
    // Process children
    if (route.firstChild) {
      this.addParamsFromRoute(route.firstChild);
    }
    
    // Process siblings
    route.children.forEach(child => {
      this.addParamsFromRoute(child);
    });
  }

  private createBreadcrumbs(): MenuItem[] {
    const breadcrumbs: MenuItem[] = [];
    
    // Get the current activated route
    let currentRoute: ActivatedRoute = this.activatedRoute;
    while (currentRoute.firstChild) {
      currentRoute = currentRoute.firstChild;
    }
    
    // Process the current route
    this.processRoute(currentRoute, breadcrumbs);
    
    return breadcrumbs;
  }
  
  private processRoute(route: ActivatedRoute, breadcrumbs: MenuItem[]): void {
    if (!route) return;
    
    // If this route has breadcrumb data
    if (route.snapshot.data['breadcrumb']) {
      // Create breadcrumb item for current route
      const currentUrl = this.router.url;
      const currentItem: MenuItem = {
        label: route.snapshot.data['breadcrumb'],
        routerLink: currentUrl
      };
      
      // Add to the beginning of the array
      breadcrumbs.unshift(currentItem);
      
      // Process parent if exists
      if (route.snapshot.data['parent']) {
        this.processParent(route.snapshot.data['parent'], breadcrumbs);
      }
    }
  }
  
  private processParent(parentPath: string, breadcrumbs: MenuItem[]): void {
    if (!parentPath) return;
    
    // Resolve any parameters in the parent path
    const resolvedPath = this.resolveParameters(parentPath);
    
    // Find the route configuration for this path
    const route = this.findRouteByPattern(parentPath);
    if (route && route.data?.breadcrumb) {
      // Create breadcrumb item for the parent
      const parentItem: MenuItem = {
        label: route.data.breadcrumb,
        routerLink: resolvedPath
      };
      
      // Add to the beginning of the array
      breadcrumbs.unshift(parentItem);
      
      // Process grandparent if exists
      if (route.data.parent) {
        this.processParent(route.data.parent, breadcrumbs);
      }
    }
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
  
  private findRouteByPattern(pathPattern: string): any {
    if (!pathPattern) return null;
    
    // Remove leading slash if present
    const normalizedPattern = pathPattern.startsWith('/') ? pathPattern.substring(1) : pathPattern;
    
    // Convert parameters to regex pattern
    const regexPattern = normalizedPattern
      .replace(/\//g, '\\/') // Escape slashes
      .replace(/:[a-zA-Z0-9]+/g, '[^\\/]+'); // Replace params with wildcard pattern
    
    for (const route of this.router.config) {
      if (!route.path) continue;
      
      // Direct match
      if (route.path === normalizedPattern) {
        return route;
      }
      
      // Pattern match
      if (new RegExp(`^${regexPattern}$`).test(route.path) || 
          new RegExp(`^${route.path.replace(/:[a-zA-Z0-9]+/g, '[^\\/]+')}$`).test(normalizedPattern)) {
        return route;
      }
    }
    
    return null;
  }
}