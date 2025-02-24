using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class WorkSpaceOptions
{

    
    public int MaxBlocks = -1;
    public bool ReadOnly = false;
    public bool Synchronous = false;

    public WorkSpaceOptions Options { get; private set; }
}
