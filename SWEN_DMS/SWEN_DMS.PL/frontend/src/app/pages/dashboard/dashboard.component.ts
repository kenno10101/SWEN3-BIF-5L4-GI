import { Component, OnInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { JsonPipe } from '@angular/common';
import { environment } from '../../../environments/evironment';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [JsonPipe],
  template: `
    <main style="padding:16px; max-width:920px; margin:0 auto; font-family:system-ui, sans-serif">
      <h1 style="margin:0 0 12px">Dashboard works</h1>
      <p style="margin:0 0 24px; opacity:.8">
        Demo loads a Document from API (GET {{environment.apiBaseUrl}}/Document/:id).
      </p>

      @if (loading) {
        <p>Loading</p>
      }

      @if (error) {
        <p style="color:#c00">Error: {{ error }}</p>
      }

      @if (data) {
        <h2>Respond</h2>
        <pre style="background:#f6f6f6; padding:12px; border-radius:8px; overflow:auto">
{{ data | json }}
        </pre>
      }
    </main>
  `
})
export class DashboardComponent implements OnInit {
  loading = false;
  error: string | null = null;
  data: unknown = null;

  protected readonly environment = environment;

  constructor(private http: HttpClient) {}

  ngOnInit(): void {
    this.fetchSample();
  }

  private fetchSample(): void {
    this.loading = true;
    this.error = null;
    this.data = null;

    // Test-ID von Dokument in DB
    const testId = '74311986-c741-4c3b-82cb-2a657fe57e04';

    this.http.get(`${environment.apiBaseUrl}/Document/${testId}`).subscribe({
      next: (res) => {
        this.data = res;
        this.loading = false;
      },
      error: (err) => {
        this.error = err?.message ?? 'Unbekannter Fehler';
        this.loading = false;
      }
    });
  }
}
