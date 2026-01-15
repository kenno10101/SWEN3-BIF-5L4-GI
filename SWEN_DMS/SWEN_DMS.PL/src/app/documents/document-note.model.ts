export interface DocumentNoteDto {
  id: string;
  documentId: string;
  content: string;
  createdAtUtc: string;
}

export interface DocumentNoteCreateDto {
  content: string;
}
