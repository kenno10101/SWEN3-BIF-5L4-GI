namespace SWEN_DMS.DTOs.Messages;

public class DeleteDocumentMessage
{
    public string Action { get; set; } = "delete";
    public Guid DocumentId { get; set; }
}