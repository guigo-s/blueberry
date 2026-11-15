# 🫐 Blueberry

<p align="center">
  <img src="apple_blueberry_raw.png" width="128" height="128" alt="Blueberry Icon" />
</p>

<p align="center">
  <strong>Transformador e editor de arquivos inteligente para Windows com design Apple Liquid Glass.</strong><br>
  Inspirado no aplicativo <em>Tangerine</em> do macOS. 100% offline, rápido e nativo.
</p>

<p align="center">
  <img src="https://img.shields.io/badge/Platform-Windows%2010%20%7C%2011-blue?style=flat-square&logo=windows" alt="Platform" />
  <img src="https://img.shields.io/badge/.NET-8.0%20WPF-512BD4?style=flat-square&logo=dotnet" alt=".NET 8" />
  <img src="https://img.shields.io/badge/Engine-FFmpeg%20Local-007808?style=flat-square&logo=ffmpeg" alt="FFmpeg" />
  <img src="https://img.shields.io/badge/License-MIT-black?style=flat-square" alt="License" />
</p>

---

## ✨ Como Funciona

O **Blueberry** roda silenciosamente em segundo plano. Basta arrastar qualquer arquivo no seu computador e usar os atalhos:

### 1. Roda de Conversão de Formatos (`Shift` + Arraste)
Segure a tecla **`Shift`** enquanto arrasta qualquer arquivo no Windows Explorer ou na Área de Trabalho. Uma roda circular em **Liquid Glass** surge ao redor do cursor com opções de conversão:
- **Imagens:** `JPG`, `PNG`, `WEBP`, `PDF`, `ICO`, `GIF`, `BMP`, `TIFF`
- **Vídeos:** `MP4`, `GIF` (alta definição), `MP3` (extrair áudio), `WEBM`, `MOV`, `MKV`, `AVI`
- **Áudios:** `MP3` (320kbps), `WAV` (estúdio), `M4A` (AAC), `FLAC`, `OGG`
- **Documentos:** `PDF`, `TXT`, `ZIP`

Solte o botão do mouse sobre o formato desejado e o arquivo convertido é salvo **ao lado do arquivo original**!

### 2. Ferramentas Rápidas de Edição (`Ctrl + Shift` + Arraste)
Segure **`Ctrl + Shift`** para acessar as ferramentas avançadas:
- **📸 Fotos & Imagens:**
  - `CORTAR (CROP)`: Abre a telinha interativa para cortar com proporções `1:1 (Instagram)`, `9:16 (Stories/Reels)`, `16:9` ou `4:3`.
  - `COMPRIMIR`: Reduz de 50% a 80% do peso do arquivo sem perda visível de qualidade.
  - `LIMPAR EXIF`: Remove dados confidenciais de localização (GPS) e câmera.
  - `P&B`: Converte em preto e branco.
  - `REDUZIR 50%`: Reduz a resolução pela metade.
  - `GIRAR 90°`: Rotaciona em 90 graus no sentido horário.
- **🎬 Vídeos:**
  - `CORTAR TEMPO (TRIM)`: Abre o editor modal com player de vídeo e sliders para cortar trechos com precisão em 0.1s.
  - `CORTAR TAMANHO (CROP)`: Corta o enquadramento do vídeo em `9:16`, `1:1`, etc.
  - `SNAPSHOT`: Salva uma foto nítida de capa do vídeo.
  - `COMPRIMIR`: Otimiza para envio no Discord e WhatsApp.
  - `MUDO`: Remove a faixa de áudio.
  - `EXTRAIR MP3`: Extrai a trilha sonora em áudio puro.
  - `VELOCIDADE 2X`: Acelera o vídeo mantendo áudio sincronizado.
  - `CRIAR GIF`: Transforma o vídeo em GIF animado nítido.
- **🎵 Áudios:**
  - `CORTAR TEMPO (TRIM)`: Cortador de trecho de áudio.
  - `NORMALIZAR`: Nivelamento de volume profissional.
  - `COMPRIMIR`: Reduz taxa de bits para 96kbps.

### 3. Cancelamento sem Ação
Se mudar de ideia enquanto a roda estiver aberta, basta **puxar o mouse de volta para o centro** (`Solte p/ cancelar`). Nenhum arquivo é modificado ou copiado!

---

## 🔒 100% Offline & Seguro

Nenhum arquivo sai da sua máquina. O processamento é realizado localmente com o **FFmpeg**, garantindo velocidade máxima e privacidade total.

---

## 🚀 Como Executar

1. Certifique-se de ter o [.NET 8 SDK](https://dotnet.microsoft.com/) e o [FFmpeg](https://ffmpeg.org/) instalados.
2. Clone o repositório:
   ```bash
   git clone https://github.com/SEU-USUARIO/blueberry.git
   cd blueberry
   ```
3. Compile e execute:
   ```bash
   dotnet run
   ```
   *(Ou execute `Iniciar Blueberry.bat` para rodar a versão compilada em segundo plano)*.

---

## 📄 Licença

Distribuído sob a licença [MIT](LICENSE).
