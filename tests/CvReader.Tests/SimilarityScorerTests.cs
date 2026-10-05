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
    public void Similarity_above_ceiling_is_hundred()
    {
        Assert.Equal(100, SimilarityScorer.ToScore(0.90));
    }

    [Fact]
    public void Similarity_in_range_is_scaled_linearly()
    {
        var middle = (SimilarityScorer.Floor + SimilarityScorer.Ceiling) / 2;

        Assert.Equal(50, SimilarityScorer.ToScore(middle));
    }
}
