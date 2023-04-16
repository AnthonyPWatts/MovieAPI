using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using System.Data;
using System.Net;

namespace Movies.Application.Database;

public class DbInitializer
{
    private readonly IDbConnectionFactory _dbConnectionFactory;

    public DbInitializer(IDbConnectionFactory dbConnectionFactory)
    {
        _dbConnectionFactory = dbConnectionFactory;
    }

    public async Task InitializeAsync()
    {
        await CreateMoviesTableAsync();
        await CreateGenresTableAsync();
        await SeedMoviesTableAsync();
        await SeedGenresTableAsync();
    }

    private async Task SeedGenresTableAsync()
    {
        string seedGenresTableSql = @"
            MERGE INTO Genres AS t
            USING (VALUES
                 ('0e2aeb82-8e2c-4ccf-9d79-87bf8a8a82c4', 'Crime'),
                 ('0e2aeb82-8e2c-4ccf-9d79-87bf8a8a82c4', 'Drama'),

                 ('76a3c8f8-98f1-4a3e-bb27-3340dcf2c5a3', 'Drama'),
                 ('76a3c8f8-98f1-4a3e-bb27-3340dcf2c5a3', 'Crime'),

                 ('dc5b5c08-9d2b-4668-87a7-79bc72e4966c', 'Action'),
                 ('dc5b5c08-9d2b-4668-87a7-79bc72e4966c', 'Crime'),
                 ('dc5b5c08-9d2b-4668-87a7-79bc72e4966c', 'Drama'),

                 ('5c45d5b5-5f05-4a90-8c5c-57af6d259c6f', 'Drama'),
                 ('5c45d5b5-5f05-4a90-8c5c-57af6d259c6f', 'Romance'),

                 ('31549f9d-ba98-4ecf-a8e4-606c0eef04c4', 'Adventure'),
                 ('31549f9d-ba98-4ecf-a8e4-606c0eef04c4', 'Drama'),
                 ('31549f9d-ba98-4ecf-a8e4-606c0eef04c4', 'Fantasy'),

                 ('89c4c9d9-739a-4a16-ae1b-3d955ae1fbf2', 'Action'),
                 ('89c4c9d9-739a-4a16-ae1b-3d955ae1fbf2', 'Adventure'),
                 ('89c4c9d9-739a-4a16-ae1b-3d955ae1fbf2', 'Sci-Fi'),

                 ('61b6d21c-8b43-436c-b89d-f66d773f6e8b', 'Crime'),
                 ('61b6d21c-8b43-436c-b89d-f66d773f6e8b', 'Drama'),

                 ('3d68b6c1-6ed8-4e99-a3e3-f19b92d49705', 'Action'),
                 ('3d68b6c1-6ed8-4e99-a3e3-f19b92d49705', 'Sci-Fi'),

                 ('c15eeb5a-1f70-4eb4-93df-c4e11d42ad13', 'Biography'),
                 ('c15eeb5a-1f70-4eb4-93df-c4e11d42ad13', 'Crime'),
                 ('c15eeb5a-1f70-4eb4-93df-c4e11d42ad13', 'Drama'),

                 ('0504b4a4-dede-4d4e-bbaa-7f4653cb61da', 'Crime'),
                 ('0504b4a4-dede-4d4e-bbaa-7f4653cb61da', 'Drama'),
                 ('0504b4a4-dede-4d4e-bbaa-7f4653cb61da', 'Thriller')
               ) AS s (MovieId, Name)
            ON t.MovieId = s.MovieId AND t.Name = s.Name
            WHEN NOT MATCHED THEN
                INSERT (MovieId, Name)
                VALUES (s.MovieId, s.Name);
        ";

        await RunCommandTextAsync(seedGenresTableSql);
    }

    private async Task CreateMoviesTableAsync()
    {
        string createTableSql = @"
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Movies')
            BEGIN
                CREATE TABLE Movies (
                    Id UNIQUEIDENTIFIER PRIMARY KEY,
                    Title VARCHAR(255),
                    Slug VARCHAR(255) UNIQUE,
                    YearOfRelease INT
                );
            END
        ";

        await RunCommandTextAsync(createTableSql);
    }

    private async Task CreateGenresTableAsync()
    {
        string createTableSql = @"
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Genres')
            BEGIN
                CREATE TABLE Genres
                (
                    MovieId UNIQUEIDENTIFIER REFERENCES Movies (Id),
                    Name NVARCHAR(MAX) NOT NULL
                );
            END
        ";

        await RunCommandTextAsync(createTableSql);
    }

    private async Task SeedMoviesTableAsync()
    {
        string seedMoviesTableSql = @"
            MERGE INTO Movies AS t
            USING (VALUES
                     ('0e2aeb82-8e2c-4ccf-9d79-87bf8a8a82c4', 'The Godfather', 'the-godfather-1972', 1972),
                     ('76a3c8f8-98f1-4a3e-bb27-3340dcf2c5a3', 'The Shawshank Redemption', 'the-shawshank-redemption-1994', 1994),
                     ('dc5b5c08-9d2b-4668-87a7-79bc72e4966c', 'The Dark Knight', 'the-dark-knight-2008', 2008),
                     ('5c45d5b5-5f05-4a90-8c5c-57af6d259c6f', 'Forrest Gump', 'forrest-gump-1994', 1994),
                     ('31549f9d-ba98-4ecf-a8e4-606c0eef04c4', 'The Lord of the Rings: The Fellowship of the Ring', 'the-lord-of-the-rings-the-fellowship-of-the-ring-2001', 2001),
                     ('89c4c9d9-739a-4a16-ae1b-3d955ae1fbf2', 'Star Wars: Episode IV - A New Hope', 'star-wars-episode-iv-a-new-hope-1977', 1977),
                     ('61b6d21c-8b43-436c-b89d-f66d773f6e8b', 'Pulp Fiction', 'pulp-fiction-1994', 1994),
                     ('3d68b6c1-6ed8-4e99-a3e3-f19b92d49705', 'The Matrix', 'the-matrix-1999', 1999),
                     ('c15eeb5a-1f70-4eb4-93df-c4e11d42ad13', 'Goodfellas', 'goodfellas-1990', 1990),
                     ('0504b4a4-dede-4d4e-bbaa-7f4653cb61da', 'The Silence of the Lambs', 'the-silence-of-the-lambs-1991', 1991)
                   ) AS s (Id, Title, Slug, YearOfRelease)
            ON t.Id = s.Id
            WHEN NOT MATCHED THEN
              INSERT (Id, Title, Slug, YearOfRelease)
              VALUES (s.Id, s.Title, s.Slug, s.YearOfRelease);

        ";

        await RunCommandTextAsync(seedMoviesTableSql);
    }

    private async Task RunCommandTextAsync(string commandText)
    {
        using var connection = await _dbConnectionFactory.CreateConnectionAsync();
        using IDbCommand command = connection.CreateCommand();
        command.CommandText = commandText;
        command.CommandType = CommandType.Text;

        // To get asynchronous execution while sticking to the IdbCommand type
        await Task.Run(() => command.ExecuteNonQuery());
    }
}
