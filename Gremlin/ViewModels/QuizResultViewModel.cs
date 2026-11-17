using System;
using Gremlin.Models; 

namespace Gremlin.ViewModels
{
    public class QuizResultViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public int Score { get; set; }
        public int MaxPossibleScore { get; set; }
        public DateTime CompletedAt { get; set; }
        
        public Quiz Quiz { get; set; }
        
        public List<HistoryAnswer> Answers { get; set; } = new();
    }
}