// Code authored by Dean Edis (DeanTheCoder).
// Anyone is free to copy, modify, use, compile, or distribute this software,
// either in source code form or as a compiled binary, for any non-commercial
// purpose.
// 
// If you modify the code, please retain this copyright header,
// and consider contributing back to the repository or letting us know
// about your modifications. Your contributions are valued!
// 
// THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND.
namespace TetraCore;

/// <summary>
/// A function scope prevents variables being found in ScopeFrames lower down the stack.
/// I.e. It's a 'hard stop'.
/// Block scopes do allow lower frames to be accessed.
/// In both cases the root frame object (containing global variables) can be accessed.
/// </summary>
/// <seealso cref="ScopeFrame"/>
public enum ScopeType
{
    Global,
    Function,
    Block
}