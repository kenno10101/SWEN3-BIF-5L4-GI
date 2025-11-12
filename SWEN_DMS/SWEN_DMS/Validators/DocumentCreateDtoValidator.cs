using FluentValidation;
using SWEN_DMS.DTOs;

namespace SWEN_DMS.Validators;

public class DocumentCreateDtoValidator : AbstractValidator<DocumentCreateDto>
{
    public DocumentCreateDtoValidator()
    {
        RuleFor(x => x.File)
            .NotNull().WithMessage("A file must be uploaded.")
            .Must(file => file.Length > 0).WithMessage("Uploaded file is empty.")
            .Must(file => file.Length <= 10 * 1024 * 1024)
            .WithMessage("Maximum file size is 10 MB.")
            .Must(file => file.ContentType == "application/pdf" ||
                          file.ContentType.StartsWith("image/"))
            .WithMessage("Only PDF or image files are allowed.");

        RuleFor(x => x.Tags)
            .MaximumLength(200).WithMessage("Tags cannot exceed 200 characters.");
    }
}