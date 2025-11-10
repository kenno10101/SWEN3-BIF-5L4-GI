export interface DocumentDto {
  id: string;
  fileName: string;
  summary?: string | null;
  tags?: string | null;
  uploadedAt: string;
}
