using Dapper;
using Movies.Application.Database;
using Movies.Application.Models;
using System.Data;

namespace Movies.Application.Repositories;

public sealed class MovieRepository : IMovieRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public MovieRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<bool> CreateAsync(Movie movie, CancellationToken token)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        using var transaction = connection.BeginTransaction();

        var result  = await connection.ExecuteAsync(new CommandDefinition("""
            INSERT INTO Movies (Id, Slug, Title, YearOfRelease)
            VALUES (@Id, @Slug, @Title, @YearOfRelease)
            """, movie, transaction, cancellationToken: token));

        if (result > 0)
        {
            foreach (var genre in movie.Genres)
            {
                await connection.ExecuteAsync(new CommandDefinition("""
                    INSERT INTO Genres (movieId, Name)
                    VALUES (@movieId, @Name)
                    """, new { MovieId = movie.Id, Name = genre }, transaction, cancellationToken: token));
            }
        }

        transaction.Commit();

        return result > 0;
    }

    public async Task<bool> DeleteByIdAsync(Guid id, CancellationToken token)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        using var transaction = connection.BeginTransaction();

        await connection.ExecuteAsync(new CommandDefinition("""
            DELETE FROM Genres WHERE MovieId = @id
            """, new { id }, transaction, cancellationToken: token));

        var result = await connection.ExecuteAsync(new CommandDefinition("""
            DELETE FROM Movies WHERE Id = @id
            """, new { id }, transaction, cancellationToken: token));

        transaction.Commit();
        return result > 0;
    }

    public async Task<bool> ExistsByIdAsync(Guid id, CancellationToken token)
    {
        using var connection =await _connectionFactory.CreateConnectionAsync();
        return await connection.ExecuteScalarAsync<bool>(new CommandDefinition("""
            SELECT COUNT(1) FROM Movies WHERE Id = @id
            """, new { id }, cancellationToken: token));
    }

    public async Task<IEnumerable<Movie>> GetAllAsync(CancellationToken token)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        var result = await connection.QueryAsync(new CommandDefinition("""
            SELECT
                m.Id, m.Title, m.Slug, m.YearOfRelease,
                STRING_AGG(g.Name, ',') WITHIN GROUP (ORDER BY g.Name) AS GenreList
            FROM
                Movies m
            LEFT JOIN
                Genres g ON m.Id = g.MovieId
            GROUP BY
                m.Id, m.Title, m.Slug, m.YearOfRelease;
            """, cancellationToken: token));

        return result.Select(x => new Movie
        {
            Id = x.Id,
            Title = x.Title,
            YearOfRelease = x.YearOfRelease,
            Genres = x.GenreList != null ? 
                new List<string>(x.GenreList.Split(',')) : 
                new List<string>()
        });
    }

    public async Task<Movie?> GetByIdAsync(Guid id, CancellationToken token)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        var movie = await connection.QuerySingleOrDefaultAsync<Movie>(
            new CommandDefinition("""
                SELECT * FROM Movies WHERE Id = @id
                """, new { id }, cancellationToken: token));

        if (movie is null) 
        {
            return null;
        }

        var genres = await connection.QueryAsync<string>(
            new CommandDefinition("""
                SELECT Name FROM Genres WHERE MovieId = @id
                """, new { id }, cancellationToken: token));

        foreach (var genre in genres)
        {
            movie.Genres.Add(genre);
        }

        return movie;
    }

    public async Task<Movie?> GetBySlugAsync(string slug, CancellationToken token)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        var movie = await connection.QuerySingleOrDefaultAsync<Movie>(
            new CommandDefinition("""
                SELECT * FROM Movies WHERE Slug = @slug
                """, new { slug }, cancellationToken: token));

        if (movie is null)
        {
            return null;
        }

        var genres = await connection.QueryAsync<string>(
            new CommandDefinition("""
                SELECT Name FROM Genres WHERE MovieId = @id
                """, new { id = movie.Id }, cancellationToken: token));

        foreach (var genre in genres)
        {
            movie.Genres.Add(genre);
        }

        return movie;
    }

    public async Task<bool> UpdateAsync(Movie movie, CancellationToken token)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        using var transaction = connection.BeginTransaction();

        await connection.ExecuteAsync(new CommandDefinition("""
            DELETE FROM Genres WHERE MovieId = @id
            """, new { id = movie.Id }, transaction, cancellationToken: token));

        foreach (var genre in movie.Genres)
        {
            await connection.ExecuteAsync(new CommandDefinition("""
                INSERT INTO Genres (MovieId, Name)
                VALUES (@MovieId, @Name)
                """, new { MovieId = movie.Id, Name = genre }, transaction, cancellationToken: token));
        }

        var result = await connection.ExecuteAsync(new CommandDefinition("""
            UPDATE Movies 
            SET 
                Slug = @Slug, 
                Title = @Title, 
                YearOfRelease = @YearOfRelease
            WHERE Id = @Id
            """, movie, transaction, cancellationToken: token));

        transaction.Commit();
        return result > 0;
    }
}
