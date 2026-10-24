using System;
using System.Collections.Generic;
using System.Text;

namespace FileForge.Domain.Enums;

public enum DocumentStatus
{
    Uploaded = 0,
    Processing = 1,
    Ready = 2,
    Failed = 3
}