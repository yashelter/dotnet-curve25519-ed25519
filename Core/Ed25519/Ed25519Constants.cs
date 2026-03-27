using Core.Base;

namespace Core.Ed25519;


#if UNSAFE_ENABLED
[System.Runtime.CompilerServices.SkipLocalsInit]
#endif
public static class Ed25519Constants
{
    // Параметры кривой: -X^2 + Y^2 = 1 + d*X^2*Y^2
    public static readonly FieldElement4 D;
    public static readonly FieldElement4 D2; // 2 * d
    
    static Ed25519Constants()
    {
        // d = -121665 / 121666 
        FieldElement4 a24 = FieldElement4.A24; // 121665
        FieldElement4 one = FieldElement4.One;
        FieldElement4 zero = FieldElement4.Zero;
        
        FieldElement4 num = default;
        FieldElement4.Sub(ref num, in zero, in a24); // -121665
        
        FieldElement4 den = default;
        FieldElement4.Add(ref den, in a24, in one);  // 121666
        
        FieldElement4 denInv = default;
        FieldElement4.Invert(ref denInv, in den);
        
        FieldElement4 d = default;
        FieldElement4.Multiply(ref d, in num, in denInv);
        D = d;
        
        FieldElement4 d2 = default;
        FieldElement4.Add(ref d2, in d, in d);
        D2 = d2;
    }
}