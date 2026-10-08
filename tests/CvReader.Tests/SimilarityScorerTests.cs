using CvReader.Application.Matching;

namespace CvReader.Tests;

public class SimilarityScorerTests
{
    [Fact]
    public void Similarity_below_floor_is_zero()
    {
        Assert.Equal(0, SimilarityScorer.ToScore(0.10));
    }

    [Fact]
    public void Same_term_is_hundred()
    {
        Assert.Equal(100, SimilarityScorer.ToScore(1));
    }

    [Fact]
    public void Close_term_above_ceiling_is_capped_below_the_same_term()
    {
        Assert.Equal(SimilarityScorer.NearMatchCap, SimilarityScorer.ToScore(0.99));
    }

    [Fact]
    public void Similarity_in_range_is_scaled_linearly_up_to_the_cap()
    {
        var middle = (SimilarityScorer.Floor + SimilarityScorer.Ceiling) / 2;

        Assert.Equal(SimilarityScorer.NearMatchCap / 2, SimilarityScorer.ToScore(middle));
    }
}
