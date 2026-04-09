using Nop.Core;
using Nop.Core.Caching;
using Nop.Core.Data;
using Nop.Core.Domain.Directory;
using Nop.Services.Events;

namespace Nop.Services.Directory;

public class MeasureService : IMeasureService
{
    private const string DimensionsAllKey = "Nop.measuredimension.all";
    private const string DimensionsByIdKey = "Nop.measuredimension.id-{0}";
    private const string DimensionsPrefix = "Nop.measuredimension.";
    private const string WeightsAllKey = "Nop.measureweight.all";
    private const string WeightsByIdKey = "Nop.measureweight.id-{0}";
    private const string WeightsPrefix = "Nop.measureweight.";

    private readonly IRepository<MeasureDimension> _dimensionRepository;
    private readonly IRepository<MeasureWeight> _weightRepository;
    private readonly MeasureSettings _measureSettings;
    private readonly IStaticCacheManager _cacheManager;
    private readonly IEventPublisher _eventPublisher;

    public MeasureService(
        IRepository<MeasureDimension> dimensionRepository,
        IRepository<MeasureWeight> weightRepository,
        MeasureSettings measureSettings,
        IStaticCacheManager cacheManager,
        IEventPublisher eventPublisher)
    {
        _dimensionRepository = dimensionRepository;
        _weightRepository = weightRepository;
        _measureSettings = measureSettings;
        _cacheManager = cacheManager;
        _eventPublisher = eventPublisher;
    }

    #region Dimensions

    public virtual async Task DeleteMeasureDimensionAsync(MeasureDimension measureDimension)
    {
        ArgumentNullException.ThrowIfNull(measureDimension);
        _dimensionRepository.Delete(measureDimension);
        await _cacheManager.RemoveByPrefixAsync(DimensionsPrefix);
        await _eventPublisher.EntityDeletedAsync(measureDimension);
    }

    public virtual async Task<MeasureDimension?> GetMeasureDimensionByIdAsync(int measureDimensionId)
    {
        if (measureDimensionId == 0)
            return null;

        var key = new CacheKey(string.Format(DimensionsByIdKey, measureDimensionId), DimensionsPrefix);
        return await _cacheManager.GetAsync(key, () =>
            Task.FromResult(_dimensionRepository.GetById(measureDimensionId)));
    }

    public virtual async Task<MeasureDimension?> GetMeasureDimensionBySystemKeywordAsync(string systemKeyword)
    {
        if (string.IsNullOrEmpty(systemKeyword))
            return null;

        var dimensions = await GetAllMeasureDimensionsAsync();
        return dimensions.FirstOrDefault(d =>
            string.Equals(d.SystemKeyword, systemKeyword, StringComparison.OrdinalIgnoreCase));
    }

    public virtual async Task<IList<MeasureDimension>> GetAllMeasureDimensionsAsync()
    {
        var key = new CacheKey(DimensionsAllKey, DimensionsPrefix);
        return await _cacheManager.GetAsync(key, () =>
        {
            var query = _dimensionRepository.TableNoTracking
                .OrderBy(d => d.DisplayOrder).ThenBy(d => d.Id);
            return Task.FromResult<IList<MeasureDimension>>(query.ToList());
        }) ?? [];
    }

    public virtual async Task InsertMeasureDimensionAsync(MeasureDimension measureDimension)
    {
        ArgumentNullException.ThrowIfNull(measureDimension);
        _dimensionRepository.Insert(measureDimension);
        await _cacheManager.RemoveByPrefixAsync(DimensionsPrefix);
        await _eventPublisher.EntityInsertedAsync(measureDimension);
    }

    public virtual async Task UpdateMeasureDimensionAsync(MeasureDimension measureDimension)
    {
        ArgumentNullException.ThrowIfNull(measureDimension);
        _dimensionRepository.Update(measureDimension);
        await _cacheManager.RemoveByPrefixAsync(DimensionsPrefix);
        await _eventPublisher.EntityUpdatedAsync(measureDimension);
    }

    public virtual decimal ConvertDimension(decimal value, MeasureDimension source, MeasureDimension target, bool round = true)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);

        var result = value;
        if (result != decimal.Zero && source.Id != target.Id)
        {
            result = ConvertToPrimaryMeasureDimension(result, source);
            result = ConvertFromPrimaryMeasureDimension(result, target);
        }

        if (round)
            result = Math.Round(result, 2);

        return result;
    }

    public virtual decimal ConvertToPrimaryMeasureDimension(decimal value, MeasureDimension source)
    {
        ArgumentNullException.ThrowIfNull(source);

        var baseDimension = GetMeasureDimensionByIdAsync(_measureSettings.BaseDimensionId).GetAwaiter().GetResult();
        if (value == decimal.Zero || source.Id == baseDimension?.Id)
            return value;

        var ratio = source.Ratio;
        if (ratio == decimal.Zero)
            throw new NopException($"Exchange ratio not set for dimension [{source.Name}]");

        return value / ratio;
    }

    public virtual decimal ConvertFromPrimaryMeasureDimension(decimal value, MeasureDimension target)
    {
        ArgumentNullException.ThrowIfNull(target);

        var baseDimension = GetMeasureDimensionByIdAsync(_measureSettings.BaseDimensionId).GetAwaiter().GetResult();
        if (value == decimal.Zero || target.Id == baseDimension?.Id)
            return value;

        var ratio = target.Ratio;
        if (ratio == decimal.Zero)
            throw new NopException($"Exchange ratio not set for dimension [{target.Name}]");

        return value * ratio;
    }

    #endregion

    #region Weights

    public virtual async Task DeleteMeasureWeightAsync(MeasureWeight measureWeight)
    {
        ArgumentNullException.ThrowIfNull(measureWeight);
        _weightRepository.Delete(measureWeight);
        await _cacheManager.RemoveByPrefixAsync(WeightsPrefix);
        await _eventPublisher.EntityDeletedAsync(measureWeight);
    }

    public virtual async Task<MeasureWeight?> GetMeasureWeightByIdAsync(int measureWeightId)
    {
        if (measureWeightId == 0)
            return null;

        var key = new CacheKey(string.Format(WeightsByIdKey, measureWeightId), WeightsPrefix);
        return await _cacheManager.GetAsync(key, () =>
            Task.FromResult(_weightRepository.GetById(measureWeightId)));
    }

    public virtual async Task<MeasureWeight?> GetMeasureWeightBySystemKeywordAsync(string systemKeyword)
    {
        if (string.IsNullOrEmpty(systemKeyword))
            return null;

        var weights = await GetAllMeasureWeightsAsync();
        return weights.FirstOrDefault(w =>
            string.Equals(w.SystemKeyword, systemKeyword, StringComparison.OrdinalIgnoreCase));
    }

    public virtual async Task<IList<MeasureWeight>> GetAllMeasureWeightsAsync()
    {
        var key = new CacheKey(WeightsAllKey, WeightsPrefix);
        return await _cacheManager.GetAsync(key, () =>
        {
            var query = _weightRepository.TableNoTracking
                .OrderBy(w => w.DisplayOrder).ThenBy(w => w.Id);
            return Task.FromResult<IList<MeasureWeight>>(query.ToList());
        }) ?? [];
    }

    public virtual async Task InsertMeasureWeightAsync(MeasureWeight measureWeight)
    {
        ArgumentNullException.ThrowIfNull(measureWeight);
        _weightRepository.Insert(measureWeight);
        await _cacheManager.RemoveByPrefixAsync(WeightsPrefix);
        await _eventPublisher.EntityInsertedAsync(measureWeight);
    }

    public virtual async Task UpdateMeasureWeightAsync(MeasureWeight measureWeight)
    {
        ArgumentNullException.ThrowIfNull(measureWeight);
        _weightRepository.Update(measureWeight);
        await _cacheManager.RemoveByPrefixAsync(WeightsPrefix);
        await _eventPublisher.EntityUpdatedAsync(measureWeight);
    }

    public virtual decimal ConvertWeight(decimal value, MeasureWeight source, MeasureWeight target, bool round = true)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);

        var result = value;
        if (result != decimal.Zero && source.Id != target.Id)
        {
            result = ConvertToPrimaryMeasureWeight(result, source);
            result = ConvertFromPrimaryMeasureWeight(result, target);
        }

        if (round)
            result = Math.Round(result, 2);

        return result;
    }

    public virtual decimal ConvertToPrimaryMeasureWeight(decimal value, MeasureWeight source)
    {
        ArgumentNullException.ThrowIfNull(source);

        var baseWeight = GetMeasureWeightByIdAsync(_measureSettings.BaseWeightId).GetAwaiter().GetResult();
        if (value == decimal.Zero || source.Id == baseWeight?.Id)
            return value;

        var ratio = source.Ratio;
        if (ratio == decimal.Zero)
            throw new NopException($"Exchange ratio not set for weight [{source.Name}]");

        return value / ratio;
    }

    public virtual decimal ConvertFromPrimaryMeasureWeight(decimal value, MeasureWeight target)
    {
        ArgumentNullException.ThrowIfNull(target);

        var baseWeight = GetMeasureWeightByIdAsync(_measureSettings.BaseWeightId).GetAwaiter().GetResult();
        if (value == decimal.Zero || target.Id == baseWeight?.Id)
            return value;

        var ratio = target.Ratio;
        if (ratio == decimal.Zero)
            throw new NopException($"Exchange ratio not set for weight [{target.Name}]");

        return value * ratio;
    }

    #endregion
}
