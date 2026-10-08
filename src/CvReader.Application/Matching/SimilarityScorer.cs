namespace CvReader.Application.Matching;

public static class SimilarityScorer
{
    // bge-m3'te iki terimin cosine benzerliği: yakın anlamlılar ve çeviriler 0.73-0.93
    // ("sql server"-"sql" 0.89, "project management"-"proje yönetimi" 0.93), ilgisiz terimler 0.72'nin altında kalıyor
    // ("machine learning"-"machine operator" 0.67).
    public const double Floor = 0.72;
    public const double Ceiling = 0.95;

    // Aynı terimin vektörü de aynıdır; aradaki fark yalnızca kayan nokta hatasıdır.
    public const double Exact = 0.9999;

    // Yakın anlamlı bir terim, terimin kendisi kadar puan alamaz.
    public const double NearMatchCap = 80;

    // Ham cosine benzerliğini 0-100 arası bir skora çevirir: terimin kendisi 100, yakın anlamlılar en çok NearMatchCap.
    public static double ToScore(double similarity)
    {
        if (similarity >= Exact) return 100;

        var scaled = (similarity - Floor) / (Ceiling - Floor) * NearMatchCap;
        return Math.Round(Math.Clamp(scaled, 0, NearMatchCap), 1);
    }
}
