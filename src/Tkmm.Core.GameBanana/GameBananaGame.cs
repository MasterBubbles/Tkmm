﻿using System.Text.Json.Serialization;

namespace Tkmm.Core.GameBanana;

public class GameBananaGame
{
    [JsonPropertyName("_idRow")]
    public int Id { get; set; }
}