using BridgeGym.Models;
using BridgeGym.Models.Bridge;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BridgeGym.Data;

public class BridgeGymContext : IdentityDbContext
{
    public BridgeGymContext(DbContextOptions<BridgeGymContext> options)
        : base(options) { }

    public DbSet<GameSession> GameSessions { get; set; } = null!;
    public DbSet<BoardSet> BoardSets { get; set; } = null!;
    public DbSet<Board> Boards { get; set; } = null!;
    public DbSet<BoardHand> BoardHands { get; set; } = null!;
}
