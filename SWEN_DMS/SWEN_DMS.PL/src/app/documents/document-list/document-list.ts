import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import {RouterLink} from '@angular/router';
import { FormsModule } from '@angular/forms';
import { DocumentService } from '../document.service';
import { DocumentDto } from '../document.model';
import { SearchResultDocumentDto, SearchResultDto } from '../search-result.model';

@Component({
  selector: 'app-document-list',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule],
  templateUrl: './document-list.html',
  styleUrls: ['./document-list.scss'],
})
export class DocumentList implements OnInit {
  documents: DocumentDto[] = [];
  loading = true;
  error?: string;

  searchQuery = '';
  isSearching = false;

  searchResult?: SearchResultDto;
  searchDocuments: SearchResultDocumentDto[] = [];

  page = 1;
  pageSize = 10;

  constructor(private readonly documentService: DocumentService) {}

  ngOnInit(): void {
    this.documentService.getAll().subscribe({
      next: (docs) => {
        this.documents = docs;
        this.loading = false;
      },
      error: (err) => {
        console.error(err);
        this.error = 'Error with loading documents.';
        this.loading = false;
      },
    });
  }
  onSearch(): void {
    const q = this.searchQuery.trim();

    // Wenn leer -> zurück zur normalen Liste
    if (!q) {
      this.clearSearch();
      return;
    }

    this.loading = true;
    this.error = undefined;
    this.isSearching = true;
    this.page = 1;

    this.documentService.searchDocuments(q, this.page, this.pageSize).subscribe({
      next: (res) => {
        this.searchResult = res;
        this.searchDocuments = res.documents ?? [];
        this.loading = false;
      },
      error: (err) => {
        console.error(err);
        this.error = 'Error with search.';
        this.loading = false;
      },
    });
  }

  clearSearch(): void {
    this.isSearching = false;
    this.searchQuery = '';
    this.searchResult = undefined;
    this.searchDocuments = [];
  }

  goToPage(newPage: number): void {
    const q = this.searchQuery.trim();
    if (!q) return;

    this.loading = true;
    this.error = undefined;
    this.page = newPage;

    this.documentService.searchDocuments(q, this.page, this.pageSize).subscribe({
      next: (res) => {
        this.searchResult = res;
        this.searchDocuments = res.documents ?? [];
        this.loading = false;
      },
      error: (err) => {
        console.error(err);
        this.error = 'Error with search.';
        this.loading = false;
      },
    });
  }

}
