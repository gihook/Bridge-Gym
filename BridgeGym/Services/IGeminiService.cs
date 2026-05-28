using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using BridgeGym.Models.Bridge;

namespace BridgeGym.Services;

public interface IGeminiService
{
    Task<List<Card>?> ParseHandImageAsync(Stream imageStream);
    Task<BoardDiagramParseResult?> ParseBoardDiagramAsync(Stream imageStream);
    Task<List<BoardDiagramParseResult>?> ParseBoardDiagramsAsync(IEnumerable<Stream> imageStreams);
}
