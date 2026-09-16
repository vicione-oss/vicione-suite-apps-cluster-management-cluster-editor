# Code style guide

## C#

### Sorting in types

Members of a C# class, struct, interface and record are sorted by the following groups.

Groups are separated from each other by a line break.

1. Constants
   - Grouped by access modifier in order of `private`, `protected`, `internal`, `public` then `static`
   - Each group is sorted alphabetically based on field name
2. Static fields
   - Sorted alphabetically based on field name
3. Other fields
   - Sorted alphabetically based on field name
4. Properties
   - Grouped by first attribute
   - Groups are sorted alphabetically based on the name of the first attribute
   - Properties without attribute are put into a group without attributes, this group comes last
5. Events
   - Sorted alphabetically based on event name
6. Constructors
   - Grouped by keyword in order of `static`, `public`, `internal`, `protected`, `private` followed by constructors without keyword
   - Groups are sorted by number of parameters in ascending order
7. Methods
   - Sorted alphabetically based on method name

### Sorting in statements

1. Constructors
   - Property assignments are sorted alphabetically by property name
2. Object initializers
   - Property assignments are sorted alphabetically by property name

### Comments

Comments should be written in english. They should start with a capital letter and a whitespace, e.g. `// This is a comment.`

## git

### Merge request titles and commit messages

Merge requests are squashed on merge, so the merge request title is what ends up in the history.
It should be written in english, start with a capital letter and use imperative mood, e.g. "Fix selection rectangle breaking after drag".

Individual commit messages have no enforced convention, keep them short.
