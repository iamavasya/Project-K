import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { environment } from '../../../../environments/environment';

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

    if (!this.isProtectedBadgesImageUrl(imageUrl)) {
      return imageUrl;
    }

    const cachedObjectUrl = this.objectUrlBySourceUrl.get(imageUrl);
    if (cachedObjectUrl) {
      return cachedObjectUrl;
    }

    if (!this.pendingImageLoads.has(imageUrl)) {
      this.pendingImageLoads.add(imageUrl);
      this.http.get(imageUrl, { responseType: 'blob' }).subscribe({
        next: (blob) => {
          const objectUrl = URL.createObjectURL(blob);
          this.objectUrlBySourceUrl.set(imageUrl, objectUrl);
          this.pendingImageLoads.delete(imageUrl);
        },
        error: () => {
          this.pendingImageLoads.delete(imageUrl);
        }
      });
    }

    return null;
  }

  private isProtectedBadgesImageUrl(imageUrl: string): boolean {
    if (imageUrl.startsWith('/badges_images/')) {
      return true;
    }

    if (!this.apiOrigin) {
      return imageUrl.includes('/badges_images/');
    }

    return imageUrl.startsWith(`${this.apiOrigin}/badges_images/`);
  }

  private resolveApiOrigin(): string {
    try {
      return new URL(environment.apiUrl).origin;
    } catch {
      return '';
    }
  }
}