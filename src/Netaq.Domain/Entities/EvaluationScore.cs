using Netaq.Domain.Common;
using Netaq.Domain.Enums;

namespace Netaq.Domain.Entities;

/// <summary>
/// Records an individual committee member's evaluation score for a specific
/// criterion on a specific proposal. Supports blind evaluation where each
/// member scores independently without seeing others' scores.
/// </summary>
public class EvaluationScore : BaseEntity
{
    /// <summary>
    /// Reference to the proposal being evaluated.
    /// </summary>
    public Guid ProposalId { get; set; }

    /// <summary>
    /// Reference to the evaluation criterion (from TenderCriteria tree).
    /// </summary>
    public Guid CriteriaId { get; set; }

    /// <summary>
    /// Reference to the committee member who gave this score.
    /// </summary>
    public Guid EvaluatorUserId { get; set; }

    /// <summary>
    /// The score given by this evaluator for this criterion (0-100).
    /// </summary>
    public decimal Score { get; set; }

    /// <summary>
    /// Mandatory justification when score is below the passing threshold.
    /// </summary>
    public string? Justification { get; set; }

    /// <summary>
    /// Whether this score was finalized by the committee chair.
    /// </summary>
    public bool IsFinalized { get; set; } = false;

    /// <summary>
    /// The unified/finalized score set by the committee chair (after review).
    /// Null until the chair finalizes.
    /// </summary>
    public decimal? FinalizedScore { get; set; }

    /// <summary>
    /// Chair's notes on why the score was adjusted (if different from original).
    /// </summary>
    public string? FinalizationNotes { get; set; }

    /// <summary>
    /// AI-suggested score for this criterion (advisory only).
    /// </summary>
    public decimal? AiSuggestedScore { get; set; }

    /// <summary>
    /// AI-generated justification for the suggested score.
    /// </summary>
    public string? AiJustification { get; set; }

    /// <summary>
    /// Type of evaluation (Technical or Financial).
    /// </summary>
    public CriteriaType EvaluationType { get; set; } = CriteriaType.Technical;

    // Navigation properties
    public Proposal Proposal { get; set; } = null!;
    public TenderCriteria Criteria { get; set; } = null!;
    public User EvaluatorUser { get; set; } = null!;
}
