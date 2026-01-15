import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../config/api-base-url.token';
import { DocumentDto } from './document.model';
import { SearchResultDto } from './search-result.model';

@Injectable({ providedIn: 'root' })
export class DocumentService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);
  private readonly documentUrl = `${this.baseUrl}/Document`; // -> GET /api/Document
  private readonly searchUrl = `${this.baseUrl}/Search`; // -> GET /api/Search


  getAll(): Observable<DocumentDto[]> {
    return this.http.get<DocumentDto[]>(this.documentUrl);
  }

  getById(id: string): Observable<DocumentDto> {
    return this.http.get<DocumentDto>(`${this.documentUrl}/${id}`);
  }

  uploadDocument(file: File, tags?: any): Observable<DocumentDto> {
    const formData = new FormData();
    formData.append('file', file);

    if (tags) {
      formData.append('tags', tags);
    }
    return this.http.post<DocumentDto>(`${this.documentUrl}/upload`, formData);
  }

  searchDocuments(query: string, page = 1, pageSize = 10): Observable<SearchResultDto> {
    const params = new HttpParams()
      .set('q', query)
      .set('page', page)
      .set('pageSize', pageSize);

    return this.http.get<SearchResultDto>(this.searchUrl, { params });
  }


  deleteDocument(id: string): Observable<void> {
    return this.http.delete<void>(`${this.documentUrl}/${id}`);
  }
}
