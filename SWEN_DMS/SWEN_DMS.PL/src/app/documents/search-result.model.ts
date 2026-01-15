export interface SearchResultDto {
  totalHits: number;
  documents: SearchResultDocumentDto[];
}

export interface SearchResultDocumentDto {
  documentId: string;
  fileName: string;
  extractedText?: string | null;
  summary?: string | null;
  tags: string[];
  uploadedAt: string;
  score: number;
  highlights?: Record<string, string[]> | null;
}
