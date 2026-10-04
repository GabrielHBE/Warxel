# Voxel Studio

Editor de mapas voxel integrado à Unity 6 / URP. Abra **Tools > Warxel > Voxel Studio** ou o botão **Abrir Voxel Studio** no Inspector de um `VoxelTerrain`.

## Começar

1. Clique em **Novo mapa vazio**, escolha uma cor e ative a edição.
2. Na Scene View, clique na grade para adicionar o primeiro voxel. Clique nas faces para continuar a construção.
3. Escolha **Caixa**, **Esfera**, **Cilindro** ou **Plano XZ** e configure as dimensões. Clique para carimbar; arraste para aplicar um traço contínuo.
4. Salve a cena normalmente (`Ctrl+S`). **Salvar cópia .asset** cria uma cópia reutilizável; **Abrir como novo mapa** instancia os dados desse asset em outro objeto, sem editar o original.

As dimensões vão de 1 a 64 por eixo e aceitam tamanhos pares exatos. A esfera aceita dimensões diferentes para criar elipsoides. A forma oca aplica apenas a casca; no plano, o contorno. O pincel fica centrado na célula apontada, com tamanhos pares estendendo uma célula adicional no sentido positivo. Assim, carimbos grandes podem penetrar uma superfície existente; use a camada Y para posicionar com precisão.

## Controles

| Ação | Atalho na Scene View |
| --- | --- |
| Adicionar voxels | B |
| Apagar voxels | R |
| Pintar voxels existentes | P |
| Capturar a cor de um voxel | I |
| Preencher voxels conectados da mesma cor | G |
| Selecionar região | M |
| Selecionar uma nova região | Q |
| Mover os voxels selecionados com setas | W |
| Rotacionar os voxels selecionados com anéis | E |
| Diminuir / aumentar dimensões | [ / ] |
| Desfazer / refazer | Ctrl/Cmd+Z / atalhos padrão da Unity |
| Sair da edição | Esc |
| Navegar com a câmera | Alt + mouse / controles habituais da Unity |

Os atalhos só são capturados com edição ativa na Scene View. Todos os modos também têm botões com texto. A paleta mostra números, indicador da cor atual e código hexadecimal no tooltip; aceita até 64 cores. O seletor permite cores RGB personalizadas; os voxels são opacos.

Durante um traço, o pincel fica preso ao plano da face inicial para impedir que cresça na direção da câmera. **Fixar no plano XZ** usa a **Camada Y** do mapa, mesmo quando há voxels na frente. A grade é local ao mapa, com origem dos voxels no centro da célula. Translação, rotação e escala do objeto são consideradas no desenho e na seleção.

## Seleção

Arraste com **Selecionar > Região [Q]** para marcar uma região. Ajuste **Mínimo** e **Máximo** para incluir profundidade e altura. Ao soltar o mouse, os voxels existentes nessa região, inclusive internos, passam a compor a seleção. O painel mostra sua quantidade; seleções de até 512 voxels também mostram o contorno de cada voxel. Alterar os limites recaptura os voxels daquela região.

Ao terminar uma seleção não vazia, as três setas aparecem **automaticamente** na Scene View: **X vermelho, Y verde e Z azul**, com identificação de cada eixo. Segure uma seta e arraste: somente a coordenada daquele eixo muda. Os controles permanecem visíveis mesmo quando o pivô está dentro da geometria. **W** volta às setas; **E** ou **Girar** exibe os anéis de rotação X/Y/Z. Use **Q** para selecionar outra região. O deslocamento segue a grade e as rotações são de **90°** para preservar contagens, cores e posições inteiras. O pequeno cubo amarelo marca o pivô: a célula central inferior da região inicial. Ele permanece estável durante as rotações e acompanha os deslocamentos; isso permite reverter uma rotação sem deriva, inclusive em seleções de dimensões pares.

Durante o arrasto, a seleção original permanece intacta e uma prévia verde mostra o destino. Destinos inválidos aparecem em vermelho. Soltar o mouse aplica uma transformação válida em uma única operação de Undo; **Esc** cancela apenas a prévia. Acima de 512 voxels, a prévia usa a caixa envolvente. As setas e anéis seguem os eixos locais do mapa, inclusive quando o objeto possui rotação ou escala.

Também é possível usar **Deslocamento > Mover seleção / Duplicar seleção** e os botões **−90° / +90°** de cada eixo. Destinos ocupados por voxels fora da seleção bloqueiam a operação inteira por padrão. Ative **Substituir no destino** se desejar sobrescrevê-los. Movimentos que se sobrepõem às posições originais da própria seleção são permitidos.

Após mover, girar ou duplicar, apenas os voxels transformados continuam selecionados, sem incorporar voxels não selecionados dentro da nova caixa envolvente. **Ctrl/Cmd+Z** e **Redo** restauram tanto o mapa quanto a seleção e seu pivô. **Pintar região** e **Apagar região** agem sobre os voxels selecionados. **Limitar pincel à seleção** restringe os carimbos e o preenchimento aos limites da região. Use **Q** para voltar ao arrasto de uma nova região.

O preenchimento percorre os seis vizinhos ortogonais e para em outra cor, espaços vazios ou no limite da seleção. Operações de preenchimento acima de 262.144 células são recusadas integralmente. Saltos de mouse que excedam esse mesmo orçamento aplicam apenas o carimbo final. Pré-visualizações acima de 512 células usam a caixa envolvente para reduzir o custo de desenho.

## Dados e desempenho

- Os dados pertencem ao componente e são serializados na cena ou prefab; o asset é uma cópia independente.
- Índice por coordenada: inserção, pintura e remoção sem procurar na lista inteira.
- Chunks de 16 × 16 × 16; somente chunks alterados e vizinhos afetados na borda são reconstruídos.
- Greedy meshing real: faces coplanares da mesma cor são unidas, e faces internas, inclusive entre chunks, são removidas.
- Um objeto, mesh e collider por chunk, nunca um objeto por voxel. Meshes usam índices de 32 bits quando necessário.
- Colisores dos chunks modificados são atualizados ao terminar o traço. O apontamento usa DDA nos dados, sem depender desses colisores.
- As meshes são caches descartáveis, ocultos na hierarquia e reconstruídos ao carregar a cena e entrar em Play. Elas não são assets de mesh exportados.
- O shader URP incluso em `Resources/WarxelVoxel.shader` usa cores de vértice, iluminação principal, ambiente, sombra e neblina. Materiais opcionais precisam aceitar cores de vértice. Luzes adicionais e lightmaps não são implementados nesse shader.

O construtor anterior foi substituído. O GUID de `VoxelTerrain.cs`, `WorldVoxelData` e os campos serializados `voxels`, `voxelSize`, `voxelMaterial` foram preservados para carregar mapas existentes, inclusive o da SampleScene. Entradas antigas inativas/duplicadas são compactadas. Os componentes de mesh/collider antigos na raiz são desativados; seus assets não são apagados.

O editor não implementa importação/exportação `.vox`, animação, integração com a destruição em rede ou paridade completa com MagicaVoxel. As coordenadas válidas são de -32.767 a 32.767 por eixo. Grandes mapas continuam sujeitos ao custo de serialização, Undo e geração de malhas na thread principal; não foi introduzido streaming de mundo.

## Validação

Execute **Tools > Warxel > Validate Voxel Studio**. A suíte cria uma cena temporária, verifica dados, chunks, greedy meshing, orientação dos triângulos, bordas, colisores, picking com transformações, formas, preenchimento, Undo/Redo e salvamento/reabertura da cena. A cena temporária é removida e a cena ativa anterior é restaurada.

Também pode rodar em um projeto de teste com `Unity -batchmode -nographics -quit -projectPath <projeto> -executeMethod VoxelStudioValidation.Run -logFile <log>`.
