import { Routes } from '@angular/router';
import { DocumentList } from './documents/document-list/document-list';
import { DocumentDetail } from './documents/document-detail/document-detail';

export const routes: Routes = [
  { path: '', component: DocumentList },
  { path: 'documents/:id', component: DocumentDetail },
];
