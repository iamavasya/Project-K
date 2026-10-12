import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, shareReplay } from 'rxjs';
import { environment } from '../../../../../environments/environment';

/** The phone app's install page and a QR code of it, drawn by the server (the same one MFA uses). */
export interface PhoneAppInstallCode {
  url: string;
  /** A PNG `data:` URL, ready for an `img`. */
  qrCode: string;
}

@Injectable({ providedIn: 'root' })
export class PhoneAppService {
  private readonly http = inject(HttpClient);

  /** Asked once per visit: the address does not change while the page is open. */
  readonly installCode$: Observable<PhoneAppInstallCode> = this.http
    .get<PhoneAppInstallCode>(`${environment.apiUrl}/phone-app/install-code`)
    .pipe(shareReplay({ bufferSize: 1, refCount: false }));
}
