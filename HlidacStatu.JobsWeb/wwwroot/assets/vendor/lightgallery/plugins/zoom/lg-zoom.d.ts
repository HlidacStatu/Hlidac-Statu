import { LgQuery, lgQuery } from '../../lgQuery';
import { LightGallery } from '../../lightgallery';
interface Coords {
    x: number;
    y: number;
}
interface DragAllowedAxises {
    allowX: boolean;
    allowY: boolean;
}
interface ZoomTouchEvent {
    pageX: number;
    touches: {
        pageY: number;
        pageX: number;
    }[];
    pageY: number;
}
interface PossibleCords {
    minX: number;
    minY: number;
    maxX: number;
    maxY: number;
}
export default class Zoom {
    private core;
    private settings;
    private $LG;
    private imageReset;
    zoomableTimeout: any;
    positionChanged: boolean;
    pageX: number;
    pageY: number;
    scale: number;
    containerRect: ClientRect;
    dragAllowedAxises: DragAllowedAxises;
    top: number;
    left: number;
    scrollTop: number;
    constructor(instance: LightGallery, $LG: LgQuery);
    buildTemplates(): void;
    /**
     * @desc Enable zoom option only once the image is completely loaded
     * If zoomFromOrigin is true, Zoom is enabled once the dummy image has been inserted
     *
     * Zoom styles are defined under lg-zoomable CSS class.
     */
    enableZoom(event: CustomEvent): void;
    enableZoomOnSlideItemLoad(): void;
    getDragCords(e: MouseEvent): Coords;
    getSwipeCords(e: TouchEvent): Coords;
    getDragAllowedAxises(scale: number, scaleDiff?: number): DragAllowedAxises;
    setZoomEssentials(): void;
    /**
     * @desc Image zoom
     * Translate the wrap and scale the image to get better user experience
     *
     * @param {String} scale - Zoom decrement/increment value
     */
    zoomImage(scale: number, scaleDiff: number, reposition: boolean, resetToMax: boolean): void;
    resetImageTranslate(index: number): void;
    setZoomImageSize(): void;
    /**
     * @desc apply scale3d to image and translate to image wrap
     * @param {style} X,Y and scale
     */
    setZoomStyles(style: {
        x: number;
        y: number;
        scale: number;
    }): void;
    /**
     * @param index - Index of the current slide
     * @param event - event will be available only if the function is called on clicking/taping the imags
     */
    setActualSize(index: number, event?: ZoomTouchEvent): void;
    getNaturalWidth(index: number): number;
    getActualSizeScale(naturalWidth: number, width: number): number;
    getCurrentImageActualSizeScale(): number;
    getPageCords(event?: ZoomTouchEvent): Coords;
    setPageCords(event?: ZoomTouchEvent): void;
    manageActualPixelClassNames(): void;
    beginZoom(scale: number): boolean;
    getScale(scale: number): number;
    init(): void;
    zoomIn(): void;
    resetZoom(index?: number): void;
    getTouchDistance(e: TouchEvent): number;
    pinchZoom(): void;
    touchendZoom(startCoords: Coords, endCoords: Coords, allowX: boolean, allowY: boolean, touchDuration: number): void;
    getZoomSwipeCords(startCoords: Coords, endCoords: Coords, allowX: boolean, allowY: boolean, possibleSwipeCords: PossibleCords): Coords;
    private isBeyondPossibleLeft;
    private isBeyondPossibleRight;
    private isBeyondPossibleTop;
    private isBeyondPossibleBottom;
    isImageSlide(index: number): boolean;
    getPossibleSwipeDragCords(scale?: number): PossibleCords;
    setZoomSwipeStyles(LGel: lgQuery, distance: {
        x: number;
        y: number;
    }): void;
    zoomSwipe(): void;
    zoomDrag(): void;
    closeGallery(): void;
    destroy(): void;
}
export {};
