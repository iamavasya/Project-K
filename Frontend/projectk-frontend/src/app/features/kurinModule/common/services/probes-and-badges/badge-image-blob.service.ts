import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { environment } from '../../../../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class BadgeImageBlobService {
  private readonly http = inject(HttpClient);
  private readonly apiOrigin = this.resolveApiOrigin();

  private readonly objectUrlBySourceUrl = new Map<string, string>();
  private readonly pendingImageLoads = new Set<string>();

  resolveBadgeImageForDisplay(imageUrl: string | null): string | null {
    if (!imageUrl) {
      return null;
    }

    const sourceUrl = this.resolveSourceUrl(imageUrl);

    if (!this.isProtectedImageUrl(sourceUrl)) {
      return sourceUrl;
    }

    const cachedObjectUrl = this.objectUrlBySourceUrl.get(sourceUrl);
    if (cachedObjectUrl) {
      return cachedObjectUrl;
    }

    if (!this.pendingImageLoads.has(sourceUrl)) {
      this.pendingImageLoads.add(sourceUrl);
      this.http.get(sourceUrl, { responseType: 'blob' }).subscribe({
        next: (blob) => {
          const objectUrl = URL.createObjectURL(blob);
          this.objectUrlBySourceUrl.set(sourceUrl, objectUrl);
          this.pendingImageLoads.delete(sourceUrl);
        },
        error: () => {
          this.pendingImageLoads.delete(sourceUrl);
        }
      });
    }

    return null;
  }

  private isProtectedImageUrl(imageUrl: string): boolean {
    if (imageUrl.startsWith('/badges_images/')) {
      return true;
    }

    if (imageUrl.startsWith('/api/awards/images/')) {
      return true;
    }

    if (!this.apiOrigin) {
      return imageUrl.includes('/badges_images/') || imageUrl.includes('/api/awards/images/');
    }

    return imageUrl.startsWith(`${this.apiOrigin}/badges_images/`)
      || imageUrl.startsWith(`${this.apiOrigin}/api/awards/images/`);
  }

  private resolveSourceUrl(imageUrl: string): string {
    if (!this.apiOrigin) {
      return imageUrl;
    }

    if (imageUrl.startsWith('/badges_images/') || imageUrl.startsWith('/api/awards/images/')) {
      return `${this.apiOrigin}${imageUrl}`;
    }

    return imageUrl;
  }

  private resolveApiOrigin(): string {
    try {
      return new URL(environment.apiUrl).origin;
    } catch {
      return '';
    }
  }
}
