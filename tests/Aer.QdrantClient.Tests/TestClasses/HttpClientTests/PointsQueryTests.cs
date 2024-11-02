﻿using Aer.QdrantClient.Http;
using Aer.QdrantClient.Http.Filters.Builders;
using Aer.QdrantClient.Http.Models.Requests.Public.QueryPoints;
using Aer.QdrantClient.Http.Models.Requests.Public.Shared;
using Aer.QdrantClient.Http.Models.Shared;
using Aer.QdrantClient.Tests.Base;
using Aer.QdrantClient.Tests.Model;

namespace Aer.QdrantClient.Tests.TestClasses.HttpClientTests;

public class PointsQueryTests : QdrantTestsBase
{
    private QdrantHttpClient _qdrantHttpClient;

    [OneTimeSetUp]
    public void Setup()
    {
        Initialize();

        _qdrantHttpClient = ServiceProvider.GetRequiredService<QdrantHttpClient>();
    }

    [SetUp]
    public async Task BeforeEachTest()
    {
        await ResetStorage();
    }

    [Test]
    public async Task PrefetchWithoutQuery()
    {
        await PrepareCollection<TestPayload>(
                _qdrantHttpClient,
                TestCollectionName);

        var nearestPointsResponse = await _qdrantHttpClient.QueryPoints(
            TestCollectionName,
            new QueryPointsRequest(query: null)
            {
                Prefetch =
                [
                    new PrefetchPoints()
                    {
                        Filter = Q<TestPayload>.BeInRange(
                            p => p.Integer,
                            greaterThanOrEqual: 0,
                            lessThanOrEqual: 2)
                    }
                ],
                WithPayload = true,
                WithVector = true,
                Limit = 10
            },
            CancellationToken.None);

        nearestPointsResponse.Status.IsSuccess.Should().BeFalse();
        nearestPointsResponse.Status.GetErrorMessage().Should().Contain(
            "A query is needed to merge the prefetches. Can't have prefetches without defining a query.");
    }

    [Test]
    public async Task FindNearestPoints()
    {
        var (_, upsertPointsByPointIds, _) =
            await PrepareCollection<TestPayload>(
                _qdrantHttpClient,
                TestCollectionName);

        var nearestPointsByPointIdResponse = await _qdrantHttpClient.QueryPoints(
            TestCollectionName,
            new QueryPointsRequest(PointsQuery.CreateFindNearestPointsQuery(upsertPointsByPointIds.First().Value.Id))
            {
                WithPayload = true,
                WithVector = true,
                Limit = 2
            },
            CancellationToken.None);

        var nearestPointsByVectorResponse = await _qdrantHttpClient.QueryPoints(
            TestCollectionName,
            new QueryPointsRequest(PointsQuery.CreateFindNearestPointsQuery(upsertPointsByPointIds.First().Value.Vector))
            {
                WithPayload = true,
                WithVector = true,
                Limit = 2
            },
            CancellationToken.None);

        nearestPointsByPointIdResponse.Status.IsSuccess.Should().BeTrue();
        nearestPointsByPointIdResponse.Result.Points.Length.Should().Be(2);

        nearestPointsByVectorResponse.Status.IsSuccess.Should().BeTrue();
        nearestPointsByVectorResponse.Result.Points.Length.Should().Be(2);
    }

    [Test]
    public async Task FindNearestPoints_WithPrefetch()
    {
        var (_, upsertPointsByPointIds, _) =
            await PrepareCollection<TestPayload>(
                _qdrantHttpClient,
                TestCollectionName);

        var nearestPointsResponse = await _qdrantHttpClient.QueryPoints(
            TestCollectionName,
            new QueryPointsRequest(PointsQuery.CreateFindNearestPointsQuery(upsertPointsByPointIds.First().Value.Id))
            {
                Prefetch =
                [
                    new PrefetchPoints()
                    {
                        Filter = Q<TestPayload>.BeInRange(
                            p => p.Integer,
                            greaterThanOrEqual: 0,
                            lessThanOrEqual: 2)
                    }
                ],
                WithPayload = true,
                WithVector = true,
                Limit = 10
            },
            CancellationToken.None);

        nearestPointsResponse.Status.IsSuccess.Should().BeTrue();
        // less than limit since prefetch should eliminate all points but 3
        nearestPointsResponse.Result.Points.Length.Should().Be(2);

        nearestPointsResponse.Result.Points.Should().AllSatisfy(
            p =>
                p.Payload.As<TestPayload>().Integer.Should().BeInRange(0, 2)
        );
    }

    [Test]
    [TestCase(FusionAlgorithm.Rrf)]
    [TestCase(FusionAlgorithm.Dbsf)]
    public async Task Fusion(FusionAlgorithm fusionAlgorithm)
    {
        var (_, upsertPointsByPointIds, _) =
            await PrepareCollection<TestPayload>(
                _qdrantHttpClient,
                TestCollectionName);

        var nearestPointsResponse = await _qdrantHttpClient.QueryPoints(
            TestCollectionName,
            new QueryPointsRequest(PointsQuery.CreateFusionQuery(fusionAlgorithm))
            {
                Prefetch =
                [
                    new PrefetchPoints()
                    {
                        Filter = Q<TestPayload>.BeInRange(
                            p => p.Integer,
                            greaterThanOrEqual: 0,
                            lessThanOrEqual: 2),
                        Limit = 10
                    },
                    new PrefetchPoints()
                    {
                        Query = PointsQuery.CreateFindNearestPointsQuery(upsertPointsByPointIds.First().Value.Id),
                        Limit = 5
                    }
                ],
                WithPayload = true,
                WithVector = true
            },
            CancellationToken.None);

        nearestPointsResponse.Status.IsSuccess.Should().BeTrue();
        // first prefetch returns 3 points
        // second prefetch returns up to 5 points
        nearestPointsResponse.Result.Points.Length.Should().BeInRange(5, 8);
    }

    [Test]
    public async Task Sample()
    {
        await PrepareCollection<TestPayload>(
                _qdrantHttpClient,
                TestCollectionName);

        var sampleRandomPointsResponse = await _qdrantHttpClient.QueryPoints(
            TestCollectionName,
            new QueryPointsRequest(PointsQuery.CreateSampleQuery())
            {
                WithPayload = true,
                WithVector = true,
                Limit = 3
            },
            CancellationToken.None);

        sampleRandomPointsResponse.Status.IsSuccess.Should().BeTrue();
        sampleRandomPointsResponse.Result.Points.Length.Should().Be(3);
    }

    [Test]
    public async Task OrderByPoints()
    {
        await PrepareCollection<TestPayload>(
            _qdrantHttpClient,
            TestCollectionName);

        // order by does not work without indexes
        await _qdrantHttpClient.CreatePayloadIndex(
            TestCollectionName,
            "integer",
            PayloadIndexedFieldType.Integer,
            CancellationToken.None,
            isWaitForResult: true);

        await _qdrantHttpClient.EnsureCollectionReady(TestCollectionName, CancellationToken.None);

        var orderedPointsResponse = await _qdrantHttpClient.QueryPoints(
            TestCollectionName,
            new QueryPointsRequest(PointsQuery.CreateOrderByQuery(OrderBySelector<TestPayload>.Asc(p => p.Integer)))
            {
                Prefetch =
                [
                    new PrefetchPoints()
                    {
                        Query = PointsQuery.CreateOrderByQuery(
                            OrderBySelector<TestPayload>.Desc(p => p.Integer)),
                        Limit = 2
                    }
                ],
                WithPayload = true,
                WithVector = true
            },
            CancellationToken.None);

        orderedPointsResponse.Status.IsSuccess.Should().BeTrue();
        orderedPointsResponse.Result.Points.Length.Should().Be(2);

        var orderedPoints = orderedPointsResponse.Result.Points.OrderBy(p => p.Score).ToList();

        var firstPoint = orderedPoints.First();
        var secondPoint = orderedPoints.Skip(1).First();

        firstPoint.Payload.As<TestPayload>().Integer!.Value
            .Should().BeLessThan(secondPoint.Payload.As<TestPayload>().Integer!.Value);
    }

    [Test]
    public async Task QueryPointsBatched()
    {
        var (_, upsertPointsByPointIds, upsertPointIds) =
            await PrepareCollection<TestPayload>(
                _qdrantHttpClient,
                TestCollectionName);

        var queryResponse = await _qdrantHttpClient.QueryPointsBatched(
            TestCollectionName,
            new QueryPointsBatchedRequest(
                new QueryPointsRequest(PointsQuery.CreateFindNearestPointsQuery(upsertPointIds[0]))
                {
                    Filter =
                        Q.Must(
                            Q<TestPayload>.BeInRange(p => p.Integer, greaterThanOrEqual: 0)
                        ),
                    WithPayload = true,
                    WithVector = true,
                    Limit = 5
                },
                new QueryPointsRequest(PointsQuery.CreateFindNearestPointsQuery(upsertPointIds[1]))
                {
                    Filter =
                        Q.Must(
                            Q<TestPayload>.BeInRange(p => p.Integer, greaterThanOrEqual: 0)
                        ),
                    WithPayload = true,
                    WithVector = true,
                    Limit = 5
                }
            ),
            CancellationToken.None);

        queryResponse.Status.IsSuccess.Should().BeTrue();
        queryResponse.Result.Length.Should().Be(2);

        foreach (var readPointsForOneRequestInBatch in queryResponse.Result)
        {
            readPointsForOneRequestInBatch.Points.Length.Should().Be(5);

            foreach (var readPoint in readPointsForOneRequestInBatch.Points)
            {
                var readPointId = readPoint.Id.AsInteger();

                var expectedPoint = upsertPointsByPointIds[readPointId];

                expectedPoint.Id.AsInteger().Should().Be(readPointId);

                readPoint.Payload.As<TestPayload>().Integer.Should().Be(expectedPoint.Payload.Integer);
                readPoint.Payload.As<TestPayload>().FloatingPointNumber.Should()
                    .Be(expectedPoint.Payload.FloatingPointNumber);
                readPoint.Payload.As<TestPayload>().Text.Should().Be(expectedPoint.Payload.Text);
            }
        }
    }

    [Test]
    public async Task QueryPointsGrouped()
    {
        var vectorCount = 10;

        var (upsertPoints, _, _) =
            await PrepareCollection(
                _qdrantHttpClient,
                TestCollectionName,
                vectorCount: vectorCount,
                payloadInitializerFunction: i => new TestPayload()
                {
                    Integer = i < 5
                        ? 1
                        : 2,
                    Text = (i + 1).ToString()
                });

        var queryResponse = await _qdrantHttpClient.QueryPointsGrouped(
            TestCollectionName,
            new QueryPointsGroupedRequest(
                PointsQuery.CreateFindNearestPointsQuery(upsertPoints[0].Vector),
                groupBy: Q<TestPayload>.GetPayloadFieldName(p => p.Integer),
                groupsLimit: 2,
                groupSize: 10,
                withVector: true,
                withPayload: true),
            CancellationToken.None);

        queryResponse.Status.IsSuccess.Should().BeTrue();
        queryResponse.Result.Groups.Length.Should().Be(2); // 2 possible values of Integer payload property

        queryResponse.Result.Groups.Should()
            .AllSatisfy(g => g.Hits.Length.Should().Be(vectorCount / 2))
            .And.AllSatisfy(
                g => g.Hits.Should()
                    .AllSatisfy(h => h.Payload.Should().NotBeNull())
                    .And.AllSatisfy(h => h.Vector.Should().NotBeNull())
            );
    }
}
