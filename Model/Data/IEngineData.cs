﻿namespace Model.Data
{
    using System.Collections.Generic;

    public interface IEngineData
    {
        string Letters { get; }
        int Width { get; }
        IList<string> ExpectedWords { get; }
    }
}