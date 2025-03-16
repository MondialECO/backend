using WebApp.Models.DatabaseModels;

namespace WebApp.Models
{
    public class BlogViewModel
    {
        public BlogModel blogModel { get; set; }

        public List<BlogCommentModel> commentModel { get; set; }



    }
}
