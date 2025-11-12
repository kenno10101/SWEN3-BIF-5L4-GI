import { Component } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { DocumentService } from '../document.service';
import { DocumentDto } from '../document.model';

@Component({
  selector: 'app-document-upload',
  standalone: true,
  imports: [DatePipe, RouterLink, FormsModule],
  templateUrl: './document-upload.html',
  styleUrls: ['./document-upload.scss']
})
export class DocumentUpload {
  file?: File;
  isUploading = false;
  errorMessage = '';
  uploaded = false;
  uploadedDocument?: DocumentDto;

  constructor(private documentService: DocumentService, private router: Router) {}
  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      this.file = input.files[0];
    }
  }

  onDrop(event: DragEvent): void {
    event.preventDefault();
    if (event.dataTransfer?.files?.length) {
      this.file = event.dataTransfer.files[0];

      const fileInput = document.getElementById('fileInput') as HTMLInputElement;
      if (fileInput) fileInput.value = '';
    }
  }

  onDragOver(event: DragEvent): void {
    event.preventDefault();
  }

  upload(): void {
    if (!this.file) {
      this.errorMessage = 'Please select a file first.';
      return;
    }

    if (!['application/pdf', 'image/png', 'image/jpeg'].includes(this.file.type)) {
      this.errorMessage = 'Only PDF or image files are allowed.';
      return;
    }

    if (this.file.size > 10 * 1024 * 1024) { // 10 MB
      this.errorMessage = 'File too large (max 10 MB).';
      return;
    }

    this.isUploading = true;
    this.errorMessage = '';

    this.documentService.uploadDocument(this.file).subscribe({
      next: (uploadedDoc: DocumentDto) => {
        this.isUploading = false;
        this.file = undefined;
        if (uploadedDoc?.id) {
          this.uploaded = true;
          this.uploadedDocument = uploadedDoc;
        } else {
          this.errorMessage = 'Upload succeeded but no document ID was returned.';
        }
      },
      error: () => {
        this.isUploading = false;
        this.errorMessage = 'Upload failed. Please try again.';
      }
    });
  }

  goToDetail(): void {
    if (this.uploadedDocument?.id) {
      this.router.navigate(['/documents', this.uploadedDocument.id]);
    }
  }
}
