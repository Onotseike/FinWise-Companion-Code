using FinWise.Shared.Core.Telemetry;

namespace FinWise.Shared.Tests;

/// <summary>
/// Tests for token telemetry aggregation and measurement modes.
/// Validates that exact, estimated, and hybrid token tracking works correctly.
/// </summary>
[TestClass]
public sealed class TokenTelemetryAggregationTests
{
    [TestMethod]
    public void WhenAggregatingTwoMeasurements_WithExactTokens_ShouldSumCorrectly()
    {
        // Arrange
        var supervisor = TokenMeasurement.Create(
            exactInputTokens: 100,
            exactOutputTokens: 200,
            estimatedInputTokens: 100,
            estimatedOutputTokens: 200,
            mode: TokenMeasurementMode.Exact
        );

        var downstream = TokenMeasurement.Create(
            exactInputTokens: 50,
            exactOutputTokens: 150,
            estimatedInputTokens: 50,
            estimatedOutputTokens: 150,
            mode: TokenMeasurementMode.Exact
        );

        // Act
        var total = TokenMeasurement.Aggregate(supervisor, downstream);

        // Assert
        Assert.AreEqual(150, total.ExactInputTokens, "Exact input tokens should sum");
        Assert.AreEqual(350, total.ExactOutputTokens, "Exact output tokens should sum");
        Assert.AreEqual(500, total.ExactTotalTokens, "Exact total should sum");
        Assert.IsTrue(total.ExactUsageAvailable, "Exact usage should be available");
    }

    [TestMethod]
    public void WhenAggregatingWithEstimatedTokens_ShouldCombineBothModes()
    {
        // Arrange
        var supervisor = TokenMeasurement.Create(
            exactInputTokens: 100,
            exactOutputTokens: 200,
            estimatedInputTokens: 120,
            estimatedOutputTokens: 220,
            mode: TokenMeasurementMode.Hybrid
        );

        var downstream = TokenMeasurement.Create(
            exactInputTokens: null,
            exactOutputTokens: null,
            estimatedInputTokens: 80,
            estimatedOutputTokens: 180,
            mode: TokenMeasurementMode.Estimated
        );

        // Act
        var total = TokenMeasurement.Aggregate(supervisor, downstream);

        // Assert
        Assert.AreEqual(100, total.ExactInputTokens, "Exact input should be from supervisor only");
        Assert.AreEqual(200, total.ExactOutputTokens, "Exact output should be from supervisor only");
        Assert.AreEqual(200, total.EstimatedInputTokens, "Estimated input should sum");
        Assert.AreEqual(400, total.EstimatedOutputTokens, "Estimated output should sum");
    }

    [TestMethod]
    public void WhenMeasurementIsInHybridMode_MeasuredValuesShouldUseExactIfAvailable()
    {
        // Arrange
        var measurement = TokenMeasurement.Create(
            exactInputTokens: 100,
            exactOutputTokens: 200,
            estimatedInputTokens: 150,
            estimatedOutputTokens: 250,
            mode: TokenMeasurementMode.Hybrid
        );

        // Act
        // In hybrid mode, measured should use exact when available
        long measuredInput = measurement.MeasuredInputTokens;
        long measuredOutput = measurement.MeasuredOutputTokens;

        // Assert
        Assert.AreEqual(100, measuredInput, "Measured input should prefer exact in hybrid mode");
        Assert.AreEqual(200, measuredOutput, "Measured output should prefer exact in hybrid mode");
        Assert.AreEqual(TokenMeasurementMode.Hybrid, measurement.Mode);
        Assert.IsTrue(measurement.IsMeasuredExact, "Measured should indicate exact source");
    }

    [TestMethod]
    public void WhenExactUsageNotAvailable_EstimatedShouldBeUsedInHybrid()
    {
        // Arrange
        var measurement = TokenMeasurement.Create(
            exactInputTokens: null,
            exactOutputTokens: null,
            estimatedInputTokens: 150,
            estimatedOutputTokens: 250,
            mode: TokenMeasurementMode.Hybrid
        );

        // Act
        long measuredInput = measurement.MeasuredInputTokens;
        long measuredOutput = measurement.MeasuredOutputTokens;

        // Assert
        Assert.AreEqual(150, measuredInput, "Measured input should use estimated when exact unavailable");
        Assert.AreEqual(250, measuredOutput, "Measured output should use estimated when exact unavailable");
        Assert.IsFalse(measurement.IsMeasuredExact, "Measured should indicate estimated source");
        Assert.AreEqual("hybrid (estimated)", measurement.MeasuredSource);
    }

    [TestMethod]
    public void WhenCostIsSet_ShouldPreserveAcrossAggregation()
    {
        // Arrange
        var supervisor = TokenMeasurement.Create(
            exactInputTokens: 100,
            exactOutputTokens: 200,
            estimatedInputTokens: 100,
            estimatedOutputTokens: 200,
            mode: TokenMeasurementMode.Exact,
            costUsd: 0.001m
        );

        var downstream = TokenMeasurement.Create(
            exactInputTokens: 50,
            exactOutputTokens: 150,
            estimatedInputTokens: 50,
            estimatedOutputTokens: 150,
            mode: TokenMeasurementMode.Exact,
            costUsd: 0.0005m
        );

        // Act
        var total = TokenMeasurement.Aggregate(supervisor, downstream);

        // Assert
        Assert.AreEqual(0.0015m, total.CostUsd, 0.00001m, "Cost should be summed");
    }

    [TestMethod]
    public void WhenCreatingMeasurement_DefaultsShouldBeSet()
    {
        // Arrange & Act
        var measurement = TokenMeasurement.Create(
            exactInputTokens: 100,
            exactOutputTokens: 200,
            estimatedInputTokens: 100,
            estimatedOutputTokens: 200
        );

        // Assert
        Assert.AreEqual(TokenMeasurementMode.Hybrid, measurement.Mode, "Default mode should be Hybrid");
        Assert.IsTrue(measurement.ExactUsageAvailable, "Exact usage should be available");
        Assert.AreEqual(0m, measurement.CostUsd, "Default cost should be zero");
    }

    [TestMethod]
    public void WhenAggregatingMultipleAgentHops_ShouldAccumulateCorrectly()
    {
        // Arrange - simulate 3 agent hops
        var hop1 = TokenMeasurement.Create(
            exactInputTokens: 100,
            exactOutputTokens: 200,
            estimatedInputTokens: 100,
            estimatedOutputTokens: 200,
            mode: TokenMeasurementMode.Exact
        );

        var hop2 = TokenMeasurement.Create(
            exactInputTokens: 50,
            exactOutputTokens: 100,
            estimatedInputTokens: 50,
            estimatedOutputTokens: 100,
            mode: TokenMeasurementMode.Exact
        );

        var hop3 = TokenMeasurement.Create(
            exactInputTokens: 75,
            exactOutputTokens: 150,
            estimatedInputTokens: 75,
            estimatedOutputTokens: 150,
            mode: TokenMeasurementMode.Exact
        );

        // Act - aggregate sequentially
        var total1 = TokenMeasurement.Aggregate(hop1, hop2);
        var total = TokenMeasurement.Aggregate(total1, hop3);

        // Assert
        Assert.AreEqual(225, total.ExactInputTokens, "Sum of all input tokens");
        Assert.AreEqual(450, total.ExactOutputTokens, "Sum of all output tokens");
        Assert.AreEqual(675, total.ExactTotalTokens, "Sum of all total tokens");
    }

    [TestMethod]
    public void WhenAggregatingMeasurementsInEstimatedMode_AggregateModeShouldRemainEstimated()
    {
        var first = TokenMeasurement.Create(
            exactInputTokens: null,
            exactOutputTokens: null,
            estimatedInputTokens: 10,
            estimatedOutputTokens: 20,
            mode: TokenMeasurementMode.Estimated);

        var second = TokenMeasurement.Create(
            exactInputTokens: null,
            exactOutputTokens: null,
            estimatedInputTokens: 5,
            estimatedOutputTokens: 15,
            mode: TokenMeasurementMode.Estimated);

        var total = TokenMeasurement.Aggregate(first, second);

        Assert.AreEqual(TokenMeasurementMode.Estimated, total.Mode, "Aggregate mode should preserve consistent source mode");
        Assert.AreEqual(50, total.MeasuredTotalTokens, "Measured tokens should follow estimated mode");
    }

    [TestMethod]
    public void WhenAggregatingMeasurementsWithDifferentModes_AggregateModeShouldFallbackToHybrid()
    {
        var exact = TokenMeasurement.Create(
            exactInputTokens: 10,
            exactOutputTokens: 20,
            estimatedInputTokens: 10,
            estimatedOutputTokens: 20,
            mode: TokenMeasurementMode.Exact);

        var estimated = TokenMeasurement.Create(
            exactInputTokens: null,
            exactOutputTokens: null,
            estimatedInputTokens: 5,
            estimatedOutputTokens: 15,
            mode: TokenMeasurementMode.Estimated);

        var total = TokenMeasurement.Aggregate(exact, estimated);

        Assert.AreEqual(TokenMeasurementMode.Hybrid, total.Mode, "Mixed mode aggregation should fallback to hybrid");
    }
}
