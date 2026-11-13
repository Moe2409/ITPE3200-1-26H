using System;
using Gremlin.Models; // Sørg for at denne peker til der Quiz-entiteten din ligger

namespace Gremlin.ViewModels
{
    public class QuizResultViewModel
    {
        public int Id { get; set; }
        public int Score { get; set; }
        public int MaxPossibleScore { get; set; }
        public DateTime CompletedAt { get; set; }
        
        // Bruker selve entiteten direkte uten egne DTO-er
        public Quiz Quiz { get; set; }
    }
}