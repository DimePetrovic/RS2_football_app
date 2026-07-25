namespace Comeback.Profile.Application.Tests.Persistence;

using Comeback.Profile.Infrastructure.Persistence;
using FluentAssertions;
using Xunit;

public sealed class LikePatternTests
{
    [Theory]
    [InlineData("ab", "ab")]
    [InlineData("a%b", "a\\%b")]      // % must be escaped so it is matched literally
    [InlineData("a_b", "a\\_b")]      // _ must be escaped so it is matched literally
    [InlineData("a\\b", "a\\\\b")]    // the escape char itself must be escaped first
    [InlineData("100%_x", "100\\%\\_x")]
    public void Escape_EscapesLikeMetacharacters(string input, string expected)
    {
        LikePattern.Escape(input).Should().Be(expected);
    }

    [Fact]
    public void Escape_EscapesBackslashBeforeWildcards()
    {
        // Order matters: escaping '%' first and then '\' would double-escape the injected backslash.
        LikePattern.Escape("\\%").Should().Be("\\\\\\%");
    }

    [Fact]
    public void Contains_WrapsEscapedTermInWildcards()
    {
        LikePattern.Contains("a_b").Should().Be("%a\\_b%");
    }

    [Fact]
    public void Contains_LoneWildcard_IsNeutralized()
    {
        // A search for a literal "%" must not turn into a match-everything pattern.
        LikePattern.Contains("%").Should().Be("%\\%%");
    }
}
