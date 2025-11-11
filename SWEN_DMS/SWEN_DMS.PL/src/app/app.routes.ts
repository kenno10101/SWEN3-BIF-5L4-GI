import { Routes } from '@angular/router';
import { DocumentList } from './documents/document-list/document-list';
import { DocumentDetail } from './documents/document-detail/document-detail';
import { DocumentUpload } from './documents/document-upload/document-upload';

export const routes: Routes = [
  { path: '', component: DocumentList },
  { path: 'documents/upload', component: DocumentUpload },
  { path: 'documents/:id', component: DocumentDetail },
];
