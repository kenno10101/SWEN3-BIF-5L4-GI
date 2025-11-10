import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { inject } from '@angular/core';
import { DocumentService } from '../document.service';
import { DocumentDto } from '../document.model';

@Component({
  selector: 'app-document-detail',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './document-detail.html',
  styleUrls: ['./document-detail.scss'],
})
export class DocumentDetail {
  private readonly route = inject(ActivatedRoute);
  private readonly documentService = inject(DocumentService);

  loading = true;
  error?: string;
  document?: DocumentDto;

  constructor() {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.error = 'Invalid document-ID.';
      this.loading = false;
      return;
    }

    this.documentService.getById(id).subscribe({
      next: (doc) => {
        this.document = doc;
        this.loading = false;
      },
      error: (err) => {
        console.error(err);
        this.error = 'Could not load document.';
        this.loading = false;
      },
    });
  }
}
