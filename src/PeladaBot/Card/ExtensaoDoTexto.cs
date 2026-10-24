using SixLabors.Fonts;

namespace PeladaBot.Card;

/// <summary>
/// Quanto espaço uma linha ocupa quando desenhada a partir de (0, 0). <see cref="CaixaDaLinha"/> cobre o avanço do
/// texto e, na vertical, do acento mais alto ao descendente mais profundo da fonte, igual para toda linha de um
/// tamanho. <see cref="Tinta"/> soma isso à tinta dos glifos reais, que pode ultrapassá-la. <see cref="LinhaDeBase"/>
/// é a distância da origem até a linha de base, onde as letras se apoiam.
/// </summary>
internal readonly record struct ExtensaoDoTexto(FontRectangle CaixaDaLinha, FontRectangle Tinta, float LinhaDeBase);
