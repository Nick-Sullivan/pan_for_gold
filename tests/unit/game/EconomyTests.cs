namespace pan_for_gold.Tests;

public class EconomyTests
{
    [Fact]
    public void FlowMultiplier_ZeroFlow_ReturnsZero()
        => Assert.Equal(0f, Economy.FlowMultiplier(0f, 20f));

    [Fact]
    public void FlowMultiplier_MaxFlow_ReturnsOne()
        => Assert.Equal(1f, Economy.FlowMultiplier(20f, 20f));

    [Fact]
    public void FlowMultiplier_HalfFlow_ReturnsHalf()
        => Assert.Equal(0.5f, Economy.FlowMultiplier(10f, 20f));

    [Fact]
    public void FlowMultiplier_AboveMax_ClampsToOne()
        => Assert.Equal(1f, Economy.FlowMultiplier(30f, 20f));

    [Fact]
    public void FlowMultiplier_NegativeFlow_ClampsToZero()
        => Assert.Equal(0f, Economy.FlowMultiplier(-5f, 20f));
}
