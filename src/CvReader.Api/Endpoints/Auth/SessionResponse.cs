namespace CvReader.Api.Endpoints.Auth;

// Token yanıt gövdesinde dönmez; tarayıcıya sadece HttpOnly cookie ile verilir.
public record SessionResponse(string Email);
