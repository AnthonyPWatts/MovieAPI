﻿namespace Movies.Contracts.Requests;

public class CreateMovieRequest
{
    public required string Title { get; init; }
    public required int YearOfRelease { get; init; }
    public required IEnumerable<string> Genres { get; set; } = Enumerable.Empty<string>();
}
