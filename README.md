# Blueberry

<p align="center">
  <img src="apple_blueberry_raw.png" width="128" height="128" alt="Blueberry Icon" />
</p>

<p align="center">
  <strong>Utilitário nativo para Windows de conversão e edição rápida de arquivos via gestos de arrastar e soltar.</strong><br>
  Inspirado no design Liquid Glass e na experiência do Tangerine para macOS. Totalmente offline, privativo e integrado ao sistema.
</p>

<p align="center">
  <img src="https://img.shields.io/badge/Plataforma-Windows%2010%20%7C%2011-blue?style=flat-square&logo=windows" alt="Plataforma" />
  <img src="https://img.shields.io/badge/.NET-8.0%20WPF-512BD4?style=flat-square&logo=dotnet" alt=".NET 8" />
  <img src="https://img.shields.io/badge/Processamento-FFmpeg%20Local-007808?style=flat-square&logo=ffmpeg" alt="FFmpeg" />
  <img src="https://img.shields.io/badge/Licen%C3%A7a-MIT-black?style=flat-square" alt="Licença" />
</p>

---

## Visão Geral

O Blueberry opera de forma silenciosa em segundo plano a partir da bandeja do sistema. Ao arrastar qualquer arquivo no Windows Explorer ou na Área de Trabalho com as teclas de atalho pressionadas, uma interface radial com efeito de vidro translúcido surge sob o cursor para selecionar ações imediatas.

Todos os arquivos convertidos e editados são salvos automaticamente no mesmo diretório do arquivo original.

---

## Modos de Operação

### 1. Conversão de Formatos (Shift + Arrastar)
Segure a tecla **Shift** ao arrastar um arquivo para visualizar as opções de conversão:
- **Imagens:** JPG, PNG, WEBP, PDF, ICO, GIF, BMP, TIFF.
- **Vídeos:** MP4, GIF animado, MP3 (extração de áudio), WEBM, MOV, MKV, AVI.
- **Áudios:** MP3 (320 kbps), WAV, AAC, FLAC, OGG.
- **Documentos:** PDF, TXT, ZIP.

### 2. Ferramentas de Edição Rápida (Ctrl + Shift + Arrastar)
Segure as teclas **Ctrl + Shift** ao arrastar para acessar utilitários avançados com modais interativos:
- **Imagens:**
  - **Cortar Enquadramento:** Abre a interface visual de recorte com proporções predefinidas (1:1, 9:16, 16:9, 4:3).
  - **Comprimir:** Reduz o tamanho do arquivo preservando a fidelidade visual.
  - **Limpar EXIF:** Remove coordenadas de GPS e metadados da câmera.
  - **Preto e Branco:** Converte a imagem para escala de cinza.
  - **Reduzir 50%:** Redimensiona a resolução pela metade.
  - **Girar 90°:** Aplica rotação horária de 90 graus.
- **Vídeos:**
  - **Cortar Duração:** Abre o editor com player de vídeo integrado e controles deslizantes para definir o início e o fim com precisão.
  - **Cortar Enquadramento:** Recorta o aspecto do vídeo (por exemplo, 9:16 ou 1:1).
  - **Capturar Frame:** Salva um quadro estático em alta resolução como imagem de capa.
  - **Comprimir:** Otimiza o vídeo para envio em mensageiros como Discord e WhatsApp.
  - **Mudo:** Remove a trilha sonora.
  - **Extrair Áudio:** Isola a faixa sonora em formato MP3.
  - **Acelerar 2x:** Dobra a velocidade de reprodução mantendo a sincronia do áudio.
- **Áudios:**
  - **Cortar Duração:** Recorta trechos com marcação de início e término.
  - **Normalizar Volume:** Ajusta e nivela os picos sonoros.
  - **Comprimir:** Otimiza a taxa de bits para compartilhamento rápido.

### 3. Cancelamento Seguro
Ao arrastar o cursor de volta para a área central da interface radial, a operação é cancelada sem qualquer alteração ou duplicação de arquivos.

---

## Privacidade e Processamento Local

O processamento é realizado localmente pelo motor do FFmpeg. Nenhum arquivo, imagem ou dado é transmitido para servidores externos ou serviços em nuvem.

---

## Instalação e Execução

### Opção 1: Executável Pronto (Recomendado)
Baixe o pacote pré-compilado na aba [Releases](https://github.com/guigo-s/blueberry/releases), descompacte e execute o arquivo `Blueberry.exe`.

### Opção 2: Compilação a partir do Código-Fonte
Requisitos:
- .NET 8.0 SDK
- FFmpeg instalado no sistema

```bash
git clone https://github.com/guigo-s/blueberry.git
cd blueberry
dotnet run
```

---

## Licença

Este projeto é disponibilizado sob os termos da licença [MIT](LICENSE).
