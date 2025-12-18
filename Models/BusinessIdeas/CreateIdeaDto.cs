using WebApp.Models.DatabaseModels;

namespace WebApp.Models.BusinessIdeas
{
    public class CreateIdeaDto
    {
        public string Title { get; set; }
        public string Summary { get; set; }
        public string Stage { get; set; }
        public string MarketSize { get; set; }
        public string Problem { get; set; }
        public string Solution { get; set; }
        public string RevenueModel { get; set; }
        public decimal FundingRequired { get; set; }
        public int EquityOffered { get; set; }
        public List<MilestoneDto> Milestones { get; set; }
    }
    public class MilestoneDto
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime TargetDate { get; set; }
    }
}
