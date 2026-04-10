namespace WebApp.Models.Dtos
{
    public class CreateCompanyDto
    {
        public string LegalName { get; set; } = string.Empty;
        public string? SiretNumber { get; set; }
        public string? VatNumber { get; set; }
        public string? LegalStructure { get; set; }
        public string? RegisteredAddress { get; set; }
        public string? NafCode { get; set; }
    }
}
