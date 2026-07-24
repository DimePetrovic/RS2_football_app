export const environment = {
  production: true,
  // Prazan string => svi pozivi su relativni (/api/..., /hubs/...) i idu na
  // isti origin gde je servirana aplikacija. Caddy ih proksira na gateway.
  apiUrl: '',
};
