using Nop.Core.Domain.Directory;

namespace Nop.Services.Directory;

public interface IMeasureService
{
    // Dimensions
    Task DeleteMeasureDimensionAsync(MeasureDimension measureDimension);
    Task<MeasureDimension?> GetMeasureDimensionByIdAsync(int measureDimensionId);
    Task<MeasureDimension?> GetMeasureDimensionBySystemKeywordAsync(string systemKeyword);
    Task<IList<MeasureDimension>> GetAllMeasureDimensionsAsync();
    Task InsertMeasureDimensionAsync(MeasureDimension measureDimension);
    Task UpdateMeasureDimensionAsync(MeasureDimension measureDimension);
    decimal ConvertDimension(decimal value, MeasureDimension source, MeasureDimension target, bool round = true);
    decimal ConvertToPrimaryMeasureDimension(decimal value, MeasureDimension source);
    decimal ConvertFromPrimaryMeasureDimension(decimal value, MeasureDimension target);

    // Weights
    Task DeleteMeasureWeightAsync(MeasureWeight measureWeight);
    Task<MeasureWeight?> GetMeasureWeightByIdAsync(int measureWeightId);
    Task<MeasureWeight?> GetMeasureWeightBySystemKeywordAsync(string systemKeyword);
    Task<IList<MeasureWeight>> GetAllMeasureWeightsAsync();
    Task InsertMeasureWeightAsync(MeasureWeight measureWeight);
    Task UpdateMeasureWeightAsync(MeasureWeight measureWeight);
    decimal ConvertWeight(decimal value, MeasureWeight source, MeasureWeight target, bool round = true);
    decimal ConvertToPrimaryMeasureWeight(decimal value, MeasureWeight source);
    decimal ConvertFromPrimaryMeasureWeight(decimal value, MeasureWeight target);
}
