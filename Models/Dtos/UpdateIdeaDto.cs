using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using WebApp.Models.DatabaseModels;

namespace WebApp.Models.Dtos
{
    public class UpdateIdeaDto : CreateIdeaDto
    {
        public string Id { get; set; }
        public List<InvestmentRound?> InvestmentRounds { get; set; } = new();
    }
}
