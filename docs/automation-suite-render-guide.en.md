# Automation Suite render guide

This document contains notes on rendering elements from the current Automation Suite.

## Grid

- Square cells with a side length of 10px

## Function blocks

The rendering of function blocks is based on the size of the grid cells (gc).

- Total width 16gc
- Height of a row 2gc
- Split into header and body
  - Header
    - Row name
      - Centered
    - Row engine ID / run mode
      - 6gc engine ID
      - 8gc run mode
        - 4gc run mode (`C` for change, `Y` for cyclic)
        - 4gc cycle frequency (empty unless cyclic)
      - 2gc decorative filler cell
    - 4 rows of header connectors with the FB image in the center
      - 2gc input port
      - 12gc image
      - 2gc output port
      - When a header connector is selected or is a valid drop target, the whole connector (rendered as described in body) becomes visible and the image is hidden
  - Body
    - n rows with input and output connectors
      - 2gc input port
      - 2gc pooling mode
      - 4gc input connector name
      - 6gc output connector name, right-aligned
      - 2gc output port
