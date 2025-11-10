import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../config/api-base-url.token';
import { DocumentDto } from './document.model';

@Injectable({ providedIn: 'root' })
export class DocumentService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);
  private readonly documentUrl = `${this.baseUrl}/Document`; // -> GET /api/Document

  getAll(): Observable<DocumentDto[]> {
    return this.http.get<DocumentDto[]>(this.documentUrl);
  }

  getById(id: string): Observable<DocumentDto> {
    return this.http.get<DocumentDto>(`${this.documentUrl}/${id}`);
  }
}
