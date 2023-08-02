# Static Page Builder
A simple yet powerful static page builder, allowing to create custom websites without having to deal with much configuration nonsense.

## Features
- User-defined Layouts
- Components &amp; Templates
- Referencing

## General Usage &amp; Setup
The output will be built in the folder above the input folder.
Therefore you should follow the folder structure.

Project  
┗ src  
&ensp;&ensp; ┣ .components → e.g. Header, Footer, Navigation  
&ensp;&ensp; ┣ .templates → Repeated components; take parameters e.g. Post Listing  
&ensp;&ensp; ┗ .layouts → e.g. Main, BlogPost, CV

All the other folders and `.html` files in `src` will be used to build the structure of the website.
So you can have a folder `Posts` for example which contains your posts and then maps to `https://yoursite.com/posts/someFile.html`.

The top level folder (the one which also contains `src`) will be wiped when rebuilding.  
Exceptions:
- `fon` or `fonts`
- `img` or `images`
- `res` or `resources`
- `vid` or `videos`
- `css` or `styles`
- `js` or `scripts`

Therefore all your references should be relative to the root and use any of these notations.

Other Exceptions:
- `.git`
- `CNAME`
- `favicon*`

## Creating pages & using controls
Your individual pages are just plain old HTML, with the exception that you don't need all the other hassle like `<head>` or the rest of the scaffolding.
You just build what goes inside the body, the rest is determined by the layout.

### Identifiers
To set the layout you have to add it to the beginning of your page:
```html
Layout: Landing

<!-- Your HTML goes here -->
```

While building it would now search for `src/.layouts/Landing.html` if no file by the name is found it would fall back to `src/.layouts/default.html`.

If no Layout is specified, no page will be built.

You can also add a page title the same way as you set the layout:
```html
Layout: Landing
Title: Welcome

<!-- Your HTML goes here -->
```
The order does not matter, but if your Layout already has a title, the two will be combined.
For example, the base layout has the title "My First Website" and your page has the title "Welcome", the final page title will be "Welcome | My First Website".

Important to note, at the end of the identifiers, leave an empty line that separates them from your HTML.

### Custom Identifiers &amp; References
You can also add custom identifiers to reference from other pages:
```html
Layout: Landing
Title: Welcome
CustomSummary: This is just a quick welcome message!

<!-- Your HTML goes here -->
```

If you now write a blog for example and list all your blog posts on an index page, you can reference this custom identifier:
```html
<p>
	@ref::CustomSummary("posts/welcome");
</p>
```
This will look for the file `welcome.html` inside of `src/posts/`.
If the file can be found, and an identifier named `CustomSummary` exists, it will be filled in place.
If there is no such identifier, this reference will be empty.

A blog needs to lists lot of posts, and also list them chronologically preferrably.
Let's go over that next.

### Lists
To list a number of elements, you can use the `list` control, which also makes use of a `template`.

```html
<ul>
	@list::PostListing("posts");
</ul>
```
This will now create a list of elements, based on the template found at `src/.templates/PostListing.html`.
To determine what these templates do let's go over that next.

Lists also add a custom parameter to use within templates `Location` and `LocationSuffix` which map to the relative file path of the current list item.
So with the above example, having a file "my-first-blog-post.html" would result in `Location` being "posts/my-first-blog-post" and LocationSuffix would be the same with an addtional ".html" in the end.

This can be used to reference to the post with a hyperlink.

### Templates
Templates take parameters, when using a template via list, the parameters are taken from the input files identifiers.
In our earlier example the following template
```html
<li>
	@param::CustomSummary;
</li>
```
would look at the identifiers of each of the input files and use those to fill in the blanks.

When using a template without a list, you have to pass the parameters individually.
```html
<li>
	@template::SomeWidget("param1", "param2", "param3");
</li>
```
The parameters in the template are replaced by occurence.

### Components
Components are the most simple type of control, as they are placed "as is".
```html
<div>
	@component::Navigation;
</div>
```

### Layouts
The most important part so far we skipped. The layouts themselves.
Your layout should contain your regular HTML scaffolding:
```html
Title: My First Website

<!doctype html>
<html>
	<head>
		<meta charset="utf-8">
		<meta content="My first website." property="og:title" />
		<meta content="This is my first website." property="og:description" />
		<meta content="https://myfirstwebsite.com" property="og:url" />
		<meta content="https://myfirstwebsite.com/favicon.png" property="og:image" />
		<meta content="#DC241F" data-react-helmet="true" name="theme-color" />
		<link rel="shortcut icon" href="favicon.png" type="image/png">
	</head>
	@content;
</html>
```
The only exception being that you don't put a `<title>` in your `<head>` as you use the previously used notation.
And well `@content` is where your individual pages will be inserted and built.

## Building