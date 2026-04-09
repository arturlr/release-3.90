# Plugin: Widgets.NivoSlider

## Bounded Context
Widget — homepage image slider/carousel.

## Legacy Source
- `src/Plugins/Nop.Plugin.Widgets.NivoSlider/`

## Key Entities
- Implements `IWidgetPlugin`

## External Dependencies
- Nivo Slider JavaScript library (or modern replacement)

## Migration Notes
- **Decision**: Rewrite as .NET 10 plugin
- Displays configurable image slides on homepage
- Consider replacing Nivo Slider with modern carousel (Swiper, Splide)

## Acceptance Criteria
- [ ] Displays image slider in homepage widget zone
- [ ] Admin configuration allows setting slide images and links
- [ ] Responsive design works on mobile and desktop
