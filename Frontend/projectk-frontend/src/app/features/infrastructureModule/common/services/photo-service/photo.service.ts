import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../../../../environments/environment';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class PhotoService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/photo`;

  uploadPhoto(file: FormData): Observable<string> {
    return this.http.post<string>(`${this.apiUrl}`, file) 
  }
}
