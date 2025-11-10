import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DocumentService } from '../document.service';
import { DocumentDto } from '../document.model';
import {RouterLink} from '@angular/router';

@Component({
  selector: 'app-document-list',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './document-list.html',
  styleUrls: ['./document-list.scss'],
})
export class DocumentList implements OnInit {
  documents: DocumentDto[] = [];
  loading = true;
  error?: string;

  constructor(private readonly documentService: DocumentService) {}

  ngOnInit(): void {
    this.documentService.getAll().subscribe({
      next: (docs) => {
        this.documents = docs;
        this.loading = false;
      },
      error: (err) => {
        console.error(err);
        this.error = 'Fehler beim Laden der Dokumente.';
        this.loading = false;
      },
    });
  }
}
