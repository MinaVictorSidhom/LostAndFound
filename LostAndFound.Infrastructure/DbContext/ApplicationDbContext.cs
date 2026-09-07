using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using LostAndFound.Core.Domain.IdentityEntities;

namespace LostAndFound.Infrastructure
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser,ApplicationRole,Guid>
    {
        public ApplicationDbContext(DbContextOptions options): base(options) 
        {

        }
    }
}