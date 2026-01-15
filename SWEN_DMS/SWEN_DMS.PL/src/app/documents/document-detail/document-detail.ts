import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { DocumentService } from '../document.service';
import { DocumentDto } from '../document.model';
import { DocumentNoteDto } from '../document-note.model';

@Component({
  selector: 'app-document-detail',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule],
  templateUrl: './document-detail.html',
  styleUrls: ['./document-detail.scss'],
})
export class DocumentDetail implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly documentService = inject(DocumentService);

  loading = true;
  error?: string;
  document?: DocumentDto;

  // Notes state
  documentId?: string;

  notesLoading = false;
  notesError?: string;
  notes: DocumentNoteDto[] = [];

  noteContent = '';

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.error = 'Invalid document-ID.';
      this.loading = false;
      return;
    }

    this.documentId = id;

    this.documentService.getById(id).subscribe({
      next: (doc) => {
        this.document = doc;
        this.loading = false;

        // Load notes after the document loaded successfully
        this.loadNotes();
      },
      error: (err) => {
        console.error(err);
        this.error = 'Could not load document.';
        this.loading = false;
      },
    });
  }

  private loadNotes(): void {
    if (!this.documentId) return;

    this.notesLoading = true;
    this.notesError = undefined;

    this.documentService.getNotes(this.documentId).subscribe({
      next: (notes) => {
        this.notes = notes ?? [];
        this.notesLoading = false;
      },
      error: (err) => {
        console.error(err);
        this.notesError = 'Could not load notes.';
        this.notesLoading = false;
      },
    });
  }

  addNote(): void {
    if (!this.documentId) return;

    const content = this.noteContent.trim();
    if (!content) {
      this.notesError = 'Note content cannot be empty.';
      return;
    }

    this.notesError = undefined;

    this.documentService.addNote(this.documentId, content).subscribe({
      next: (created) => {
        // Prepend so newest note appears first
        this.notes = [created, ...this.notes];
        this.noteContent = '';
      },
      error: (err) => {
        console.error(err);
        this.notesError = 'Failed to add note.';
      },
    });
  }

  deleteNote(noteId: string): void {
    const confirmDelete = confirm('Delete this note?');
    if (!confirmDelete) return;

    this.documentService.deleteNote(noteId).subscribe({
      next: () => {
        this.notes = this.notes.filter((n) => n.id !== noteId);
      },
      error: (err) => {
        console.error(err);
        this.notesError = 'Failed to delete note.';
      },
    });
  }

  deleteDocument(): void {
    if (!this.document) return;

    const confirmDelete = confirm('Are you sure you want to delete this document?');
    if (!confirmDelete) return;

    this.documentService.deleteDocument(this.document.id).subscribe({
      next: () => {
        alert('Document deleted successfully.');
        this.router.navigate(['/']); // navigate back to list
      },
      error: (err) => {
        console.error(err);
        alert('Failed to delete document.');
      },
    });
  }
}
