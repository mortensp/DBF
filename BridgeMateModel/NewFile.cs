private void updatePlayedBoards(bool first = false)
{
    if (BMRounds is null)
        return;

    using var db = new BridgeMateContext(bwsFile);

    if (first)
    {
        // ✅ Optimization 1: Initialiser BoardsPlayed baseret på SectionEntity
        foreach (var row in BMRounds)
            row.BoardsPlayed = (row.Nspair == row.SectionEntity.MissingPair || 
                                row.Ewpair == row.SectionEntity.MissingPair) 
                                    ? row.BoardsPerRound 
                                    : 0;

        // ✅ Optimization 2: Hent alle ReceivedData på én gang (ikke iterativt)
        var receivedData = db.ReceivedData
                            .Where(r => r.Erased != true)
                            .OrderBy(r => r.Id)
                            .ToList(); // Materializér én gang

        // ✅ Optimization 3: Byg index for O(1) lookups
        var roundIndex = BMRounds.ToDictionary(r => (r.Section, r.TableNo, r.Round));

        // ✅ Optimization 4: Ret op i memory før DB save
        foreach (var row in receivedData)
        {
            if (roundIndex.TryGetValue((row.Section, row.TableNo, row.Round), out var round))
                round.BoardsPlayed++;

            row.Processed4 = true;
        }
    }
    else
    {
        // ✅ Optimization 5: Samme tilgang i else-blok - brug index i stedet for FirstOrDefault()
        var unprocessedData = db.ReceivedData
                               .Where(r => r.Processed4 != true)
                               .ToList();

        var roundIndex = BMRounds.ToDictionary(r => (r.TableNo, r.Round)); // Kun TableNo + Round nødvendig her

        foreach (var data in unprocessedData)
        {
            if (roundIndex.TryGetValue((data.TableNo, data.Round), out var round))
            {
                if (data.Erased == true)
                    round.BoardsPlayed--;
                else
                    round.BoardsPlayed++;
            }

            data.Processed4 = true;
        }
    }

    // ✅ Optimization 6: Gem kun én gang
    db.SaveChanges();

    updateRoundStatus();
}
